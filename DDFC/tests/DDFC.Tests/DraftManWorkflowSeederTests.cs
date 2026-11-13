using DDFC.Infrastructure.Data;
using DDFC.Infrastructure.Persistence;
using DDFC.Infrastructure.Services;
using DDFC.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Services;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Repositories;
using System.Reflection;
using Xunit;

namespace DDFC.Tests;

public class DraftManWorkflowSeederTests
{
    [Fact]
    public async Task NewWorkflowPlacesDraftManBetweenArchitectAndThreeD()
    {
        using var provider = CreateProvider();
        var processId = await DDFCWorkflowSeeder.SeedAsync(provider);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var steps = await db.ProcessSteps.Where(step => step.ProcessId == processId).ToListAsync();
        var design = steps.Single(step => step.Name == "Architect Department \u2013 House Plan Design");
        var draft = steps.Single(step => step.Name == "Draft Man");
        var threeD = steps.Single(step => step.Name == "Architecture Department \u2013 3D Visualization");
        var structure = steps.Single(step => step.Name == "Structure Department \u2013 Structural Design");
        Assert.Equal(14, draft.Order);
        Assert.Equal(15, threeD.Order);
        Assert.True(await db.StepTransitions.AnyAsync(transition => transition.FromStepId == design.Id && transition.ToStepId == draft.Id));
        Assert.True(await db.StepTransitions.AnyAsync(transition => transition.FromStepId == draft.Id && transition.ToStepId == threeD.Id));
        Assert.True(await db.StepTransitions.AnyAsync(transition => transition.FromStepId == threeD.Id && transition.ToStepId == structure.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingThreeDWorkIsPreservedWhenReseeding(bool threeDAlreadyCompleted)
    {
        using var provider = CreateProvider();
        var processId = await DDFCWorkflowSeeder.SeedAsync(provider);
        Guid workflowId;
        using (var scope = provider.CreateScope())
        {
            var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            var domainDb = scope.ServiceProvider.GetRequiredService<DDFCDbContext>();
            var steps = await workflowDb.ProcessSteps.Where(step => step.ProcessId == processId)
                .Include(step => step.Actions).ToListAsync();
            var design = steps.Single(step => step.Name == "Architect Department \u2013 House Plan Design");
            var draft = steps.Single(step => step.Name == "Draft Man");
            var threeD = steps.Single(step => step.Name == "Architecture Department \u2013 3D Visualization");
            var soil = steps.Single(step => step.Name == "Soil Test");
            var structure = steps.Single(step => step.Name == "Structure Department \u2013 Structural Design");
            draft.Name = "Architect \u2013 Assign 3D Drafter & Upload Draft";
            draft.Order = 15;
            threeD.Order = 14;
            (await workflowDb.StepTransitions.SingleAsync(item => item.FromStepId == design.Id && item.ToStepId == draft.Id)).ToStepId = threeD.Id;
            (await workflowDb.StepTransitions.SingleAsync(item => item.FromStepId == draft.Id && item.ToStepId == threeD.Id)).ToStepId = structure.Id;
            (await workflowDb.StepTransitions.SingleAsync(item => item.FromStepId == threeD.Id && item.ToStepId == structure.Id)).ToStepId = draft.Id;

            var groupId = Guid.NewGuid();
            var request = new WorkflowEngine.Domain.Entities.Request { ProcessId = processId, Status = RequestStatus.InProgress };
            foreach (var step in steps)
            {
                var status = step.Id == soil.Id ? RequestStepStatus.Active
                    : step.Id == threeD.Id ? (threeDAlreadyCompleted ? RequestStepStatus.Completed : RequestStepStatus.Active)
                    : step.Id == draft.Id ? (threeDAlreadyCompleted ? RequestStepStatus.Active : RequestStepStatus.Pending)
                    : step.Order < 14 ? RequestStepStatus.Completed : RequestStepStatus.Pending;
                request.Steps.Add(new WorkflowEngine.Domain.Entities.RequestStep
                {
                    ProcessStepId = step.Id, Status = status,
                    ParallelGroupId = step.Id == soil.Id || step.Id == threeD.Id || (threeDAlreadyCompleted && step.Id == draft.Id) ? groupId : null,
                    ParallelCompletionMode = ParallelCompletionMode.Independent,
                    Actions = step.Actions.Select(action => new WorkflowEngine.Domain.Entities.RequestAction { StepActionId = action.Id }).ToList()
                });
            }
            workflowDb.Requests.Add(request);
            workflowId = request.Id;
            domainDb.PossessionRequests.Add(new DDFC.Domain.Entities.PossessionRequest
            {
                RequestId = "DDFC-TEST", FileNo = "F1", WorkflowRequestId = workflowId,
                Status = threeDAlreadyCompleted ? DDFC.Domain.Enums.PossessionRequestStatus.ThreeDDraftPending : DDFC.Domain.Enums.PossessionRequestStatus.ArchitectureApproved
            });
            await workflowDb.SaveChangesAsync();
            await domainDb.SaveChangesAsync();
        }

        await DDFCWorkflowSeeder.SeedAsync(provider);
        await DDFCWorkflowSeeder.SeedAsync(provider);

        using var verification = provider.CreateScope();
        var workflow = verification.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var domain = verification.ServiceProvider.GetRequiredService<DDFCDbContext>();
        var requestSteps = await workflow.RequestSteps.Include(step => step.ProcessStep)
            .Where(step => step.RequestId == workflowId).ToListAsync();
        var updatedDraft = requestSteps.Single(step => step.ProcessStep.Name == "Draft Man");
        var updatedThreeD = requestSteps.Single(step => step.ProcessStep.Name == "Architecture Department \u2013 3D Visualization");
        Assert.Equal(RequestStepStatus.Active, updatedDraft.Status);
        Assert.Equal(threeDAlreadyCompleted ? RequestStepStatus.Completed : RequestStepStatus.Pending, updatedThreeD.Status);
        Assert.NotNull(updatedDraft.ParallelGroupId);
        Assert.Equal(DDFC.Domain.Enums.PossessionRequestStatus.ThreeDDraftPending,
            (await domain.PossessionRequests.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DesignServicesAdvanceThroughReorderedSteps(bool soilFirst)
    {
        using var provider = CreateProvider();
        var processId = await DDFCWorkflowSeeder.SeedAsync(provider);
        using var scope = provider.CreateScope();
        var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var domainDb = scope.ServiceProvider.GetRequiredService<DDFCDbContext>();
        var steps = await workflowDb.ProcessSteps.Where(step => step.ProcessId == processId)
            .Include(step => step.Actions).ToListAsync();
        var group = Guid.NewGuid();
        var workflowRequest = new WorkflowEngine.Domain.Entities.Request { ProcessId = processId, Status = RequestStatus.InProgress };
        foreach (var step in steps)
        {
            var active = step.Name is "Soil Test" or "Architect Department \u2013 House Plan Design";
            workflowRequest.Steps.Add(new WorkflowEngine.Domain.Entities.RequestStep
            {
                ProcessStepId = step.Id,
                Status = active ? RequestStepStatus.Active : step.Order < 13 ? RequestStepStatus.Completed : RequestStepStatus.Pending,
                ParallelGroupId = active ? group : null,
                ParallelCompletionMode = ParallelCompletionMode.Independent,
                Actions = step.Actions.Select(action => new WorkflowEngine.Domain.Entities.RequestAction { StepActionId = action.Id }).ToList()
            });
        }
        workflowDb.Requests.Add(workflowRequest);
        await workflowDb.SaveChangesAsync();

        var customer = new DDFC.Domain.Entities.Customer { FullName = "Customer", CNIC = "1234567890123", PhoneNumber = "123" };
        var plot = new DDFC.Domain.Entities.Plot { PlotNumber = "1", SectorNo = "A", PhaseNo = "1" };
        var request = new DDFC.Domain.Entities.PossessionRequest
        {
            RequestId = "DDFC-FLOW", FileNo = "F1", Customer = customer, CustomerId = customer.Id,
            Plot = plot, PlotId = plot.Id, WorkflowRequestId = workflowRequest.Id,
            Status = DDFC.Domain.Enums.PossessionRequestStatus.ArchitectAssigned
        };
        var userId = Guid.NewGuid();
        var plan = new DDFC.Domain.Entities.ArchitecturalPlan { Request = request, RequestId = request.Id, FileUrl = "plan.pdf", UploadedBy = userId };
        domainDb.AddRange(customer, plot, request, plan);
        await domainDb.SaveChangesAsync();

        var engine = new WorkflowEngineService(new WorkflowRepository(workflowDb), NullLogger<WorkflowEngineService>.Instance);
        var service = new PossessionRequestService(domainDb, engine,
            DispatchProxy.Create<ITaskAssignmentService, SoilTestSchedulingTests.UnsupportedProxy>(),
            DispatchProxy.Create<INotificationService, SoilTestSchedulingTests.UnsupportedProxy>(),
            NullLogger<PossessionRequestService>.Instance);
        await service.ApprovePlanAsync(request.Id, plan.Id, customer.Id);
        Assert.Equal(DDFC.Domain.Enums.PossessionRequestStatus.ThreeDDraftPending, request.Status);
        var workflow = await engine.GetRequestAsync(workflowRequest.Id);
        Assert.Equal(RequestStepStatus.Active, workflow.Steps.Single(step => step.ProcessStep.Name == "Draft Man").Status);
        Assert.Equal(RequestStepStatus.Pending, workflow.Steps.Single(step => step.ProcessStep.Name == "Architecture Department \u2013 3D Visualization").Status);

        if (soilFirst)
            await engine.CompleteActionAsync(workflow.Steps.Single(step => step.ProcessStep.Name == "Soil Test").Actions.Single().Id, userId.ToString());
        var drafter = new DDFC.Domain.Entities.CadAssignment { RequestId = request.Id, CadType = "ThreeD", AssignedUserId = userId, FileUrl = "draft.dwg", CompletedAt = DateTime.UtcNow };
        var architect = new DDFC.Domain.Entities.CadAssignment { RequestId = request.Id, CadType = "ArchitectDraft", AssignedUserId = userId, FileUrl = "architect.dwg", CompletedAt = DateTime.UtcNow };
        domainDb.CadAssignments.AddRange(drafter, architect);
        await domainDb.SaveChangesAsync();
        await service.AssignThreeDDrafterAsync(request.Id, userId, userId);
        await service.CompleteThreeDDraftAsync(request.Id, userId);
        Assert.Equal(DDFC.Domain.Enums.PossessionRequestStatus.ThreeDDraftUploaded, request.Status);
        workflow = await engine.GetRequestAsync(workflowRequest.Id);
        Assert.Equal(RequestStepStatus.Active, workflow.Steps.Single(step => step.ProcessStep.Name == "Architecture Department \u2013 3D Visualization").Status);

        domainDb.ThreeDVisualizations.Add(new DDFC.Domain.Entities.ThreeDVisualization { RequestId = request.Id, FileName = "model.png", FileUrl = "model.png", FileType = "png", UploadedBy = userId });
        await domainDb.SaveChangesAsync();
        await service.CompleteThreeDAsync(request.Id, userId);
        Assert.Equal(DDFC.Domain.Enums.PossessionRequestStatus.ThreeDCompleted, request.Status);
        workflow = await engine.GetRequestAsync(workflowRequest.Id);
        var structure = workflow.Steps.Single(step => step.ProcessStep.Name == "Structure Department \u2013 Structural Design");
        if (!soilFirst)
        {
            Assert.Equal(RequestStepStatus.Pending, structure.Status);
            await engine.CompleteActionAsync(workflow.Steps.Single(step => step.ProcessStep.Name == "Soil Test").Actions.Single().Id, userId.ToString());
        }
        Assert.Equal(RequestStepStatus.Active, (await engine.GetRequestAsync(workflowRequest.Id)).Steps.Single(step => step.Id == structure.Id).Status);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<WorkflowDbContext>(options => options.UseInMemoryDatabase($"workflow-{databaseName}"));
        services.AddDbContext<DDFCDbContext>(options => options.UseInMemoryDatabase($"ddfc-{databaseName}"));
        return services.BuildServiceProvider();
    }
}