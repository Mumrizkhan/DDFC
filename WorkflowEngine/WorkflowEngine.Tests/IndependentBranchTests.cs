using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Services;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using Xunit;

namespace WorkflowEngine.Tests;

public class IndependentBranchTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BranchesAdvanceIndependentlyAndJoinOnlyAfterBothComplete(bool soilFirst)
    {
        var fixture = new WorkflowFixture(ParallelCompletionMode.Independent);
        await fixture.Complete("Admin");
        fixture.AssertActive("Soil", "Architect");

        if (soilFirst)
        {
            await fixture.Complete("Soil");
            fixture.AssertActive("Architect");
        }

        await fixture.Complete("Architect");
        fixture.AssertActive(soilFirst ? ["Design"] : ["Soil", "Design"]);
        await fixture.Complete("Design");
        fixture.AssertActive(soilFirst ? ["Draft"] : ["Soil", "Draft"]);
        await fixture.Complete("Draft");

        if (!soilFirst)
        {
            fixture.AssertActive("Soil");
            Assert.Equal(RequestStepStatus.Pending, fixture.Step("Structure").Status);
            await fixture.Complete("Soil");
        }

        fixture.AssertActive("Structure");
        Assert.Null(fixture.Step("Structure").ParallelGroupId);
    }

    [Fact]
    public async Task ExistingAllParallelModeStillWaitsForBothSteps()
    {
        var fixture = new WorkflowFixture(ParallelCompletionMode.All);
        await fixture.Complete("Admin");
        await fixture.Complete("Architect");
        fixture.AssertActive("Soil");
        Assert.Equal(RequestStepStatus.Pending, fixture.Step("Design").Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReorderedCompletedStepWaitsForBothBranches(bool soilFirst)
    {
        var fixture = new WorkflowFixture(ParallelCompletionMode.Independent);
        await fixture.Complete("Admin");
        await fixture.Complete("Architect");
        await fixture.Complete("Design");
        fixture.ReorderCompletedDesign();

        if (soilFirst)
        {
            await fixture.Complete("Soil");
            Assert.Equal(RequestStepStatus.Pending, fixture.Step("Structure").Status);
        }
        await fixture.Complete("Draft");
        if (!soilFirst)
        {
            Assert.Equal(RequestStepStatus.Pending, fixture.Step("Structure").Status);
            await fixture.Complete("Soil");
        }
        fixture.AssertActive("Structure");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReorderedDraftPrecedesDesignAndStructureWaitsForSoil(bool soilFirst)
    {
        var fixture = new WorkflowFixture(ParallelCompletionMode.Independent);
        fixture.ReorderCompletedDesign();
        await fixture.Complete("Admin");
        await fixture.Complete("Architect");
        fixture.AssertActive("Soil", "Draft");
        if (soilFirst) await fixture.Complete("Soil");
        await fixture.Complete("Draft");
        fixture.AssertActive(soilFirst ? ["Design"] : ["Soil", "Design"]);
        await fixture.Complete("Design");
        if (!soilFirst)
        {
            Assert.Equal(RequestStepStatus.Pending, fixture.Step("Structure").Status);
            await fixture.Complete("Soil");
        }
        fixture.AssertActive("Structure");
    }

    private sealed class WorkflowFixture : IWorkflowRepository
    {
        private readonly Request _request = new() { Id = Guid.NewGuid(), Status = RequestStatus.InProgress };
        private readonly WorkflowEngineService _engine;

        public WorkflowFixture(ParallelCompletionMode mode)
        {
            var names = new[] { "Admin", "Soil", "Architect", "Design", "Draft", "Structure" };
            foreach (var (name, index) in names.Select((name, index) => (name, index)))
            {
                var processStep = new ProcessStep { Id = Guid.NewGuid(), Name = name, Order = index + 1 };
                var requestStep = new RequestStep
                {
                    Id = Guid.NewGuid(), RequestId = _request.Id, Request = _request,
                    ProcessStepId = processStep.Id, ProcessStep = processStep,
                    Status = name == "Admin" ? RequestStepStatus.Active : RequestStepStatus.Pending
                };
                var stepAction = new StepAction { Id = Guid.NewGuid(), Name = "Complete" };
                requestStep.Actions.Add(new RequestAction
                {
                    Id = Guid.NewGuid(), RequestStepId = requestStep.Id, RequestStep = requestStep,
                    StepActionId = stepAction.Id, StepAction = stepAction
                });
                _request.Steps.Add(requestStep);
            }

            AddTransition("Admin", "Soil", true, mode);
            AddTransition("Admin", "Architect", true, mode);
            AddTransition("Architect", "Design");
            AddTransition("Design", "Draft");
            AddTransition("Draft", "Structure");
            AddTransition("Soil", "Structure");
            _engine = new WorkflowEngineService(this, NullLogger<WorkflowEngineService>.Instance);
        }

        private void AddTransition(string from, string to, bool parallel = false, ParallelCompletionMode mode = ParallelCompletionMode.All)
        {
            Step(from).ProcessStep.TransitionsFrom.Add(new StepTransition
            {
                FromStepId = Step(from).ProcessStepId, ToStepId = Step(to).ProcessStepId,
                IsDefault = true, IsParallel = parallel, ParallelCompletionMode = mode
            });
        }

        public RequestStep Step(string name) => _request.Steps.Single(step => step.ProcessStep.Name == name);
        public void ReorderCompletedDesign()
        {
            Step("Architect").ProcessStep.TransitionsFrom.Single().ToStepId = Step("Draft").ProcessStepId;
            Step("Draft").ProcessStep.TransitionsFrom.Single().ToStepId = Step("Design").ProcessStepId;
            Step("Design").ProcessStep.TransitionsFrom.Single().ToStepId = Step("Structure").ProcessStepId;
        }
        public Task Complete(string name) => _engine.CompleteActionAsync(Step(name).Actions.Single().Id, "tester");
        public void AssertActive(params string[] names) => Assert.Equal(names.Order(),
            _request.Steps.Where(step => step.Status == RequestStepStatus.Active).Select(step => step.ProcessStep.Name).Order());

        public Task<Request> GetRequestWithDetailsAsync(Guid requestId) => Task.FromResult(_request);
        public Task<RequestStep> GetRequestStepAsync(Guid requestStepId) => Task.FromResult(_request.Steps.Single(step => step.Id == requestStepId));
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<Process> GetProcessAsync(Guid id) => throw new NotSupportedException();
        public Task<RequestAction> GetRequestActionAsync(Guid requestActionId) => Task.FromResult(_request.Steps.SelectMany(step => step.Actions).Single(action => action.Id == requestActionId));
        public Task<Request> CreateRequestFromProcessAsync(Guid processId) => throw new NotSupportedException();
    }
}