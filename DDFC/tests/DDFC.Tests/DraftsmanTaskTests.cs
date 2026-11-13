using System.Reflection;
using System.Security.Claims;
using DDFC.API.Controllers;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using DDFC.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowEngine.Application.Interfaces;
using Xunit;

namespace DDFC.Tests;

public class DraftsmanTaskTests
{
    [Fact]
    public async Task AssignmentCreatesChildAndCompletionAttachesParentDocument()
    {
        using var fixture = new Fixture();
        var assigned = Assert.IsType<OkObjectResult>(await fixture.Controller.Assign(fixture.Assignment()));
        var child = Assert.IsType<DraftsmanTaskResponse>(assigned.Value);
        Assert.Null(child.CompletedAt);
        fixture.SetUser(fixture.Draftsman);
        var mine = Assert.IsType<OkObjectResult>(await fixture.Controller.GetMine());
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<DraftsmanTaskResponse>>(mine.Value));

        var completed = Assert.IsType<OkObjectResult>(await fixture.Controller.Complete(child.Id, new CompleteDraftsmanRequest { FileUrl = "drawing.dwg" }));
        Assert.NotNull(Assert.IsType<DraftsmanTaskResponse>(completed.Value).CompletedAt);
        var document = Assert.Single(await fixture.Db.Documents.ToListAsync());
        Assert.Equal(fixture.Request.Id, document.RequestId);
        Assert.Equal("Architect Department \u2013 House Plan Design", document.StepName);
        Assert.Equal("drawing.dwg", document.FileUrl);
        Assert.IsType<ConflictObjectResult>(await fixture.Controller.Complete(child.Id, new CompleteDraftsmanRequest { FileUrl = "again.dwg" }));
    }

    [Fact]
    public async Task ReassignmentRemovesPreviousAssigneesAccess()
    {
        using var fixture = new Fixture();
        var assigned = Assert.IsType<DraftsmanTaskResponse>(Assert.IsType<OkObjectResult>(await fixture.Controller.Assign(fixture.Assignment())).Value);
        var other = fixture.AddDraftsman();
        var reassigned = Assert.IsType<DraftsmanTaskResponse>(Assert.IsType<OkObjectResult>(await fixture.Controller.Assign(fixture.Assignment() with { AssignedToUserId = other.Id })).Value);
        Assert.Equal(assigned.Id, reassigned.Id);
        fixture.SetUser(fixture.Draftsman);
        Assert.IsType<NotFoundResult>(await fixture.Controller.Complete(assigned.Id, new CompleteDraftsmanRequest { FileUrl = "drawing.dwg" }));
        fixture.SetUser(other);
        Assert.IsType<OkObjectResult>(await fixture.Controller.Complete(assigned.Id, new CompleteDraftsmanRequest { FileUrl = "drawing.dwg" }));
    }

    [Fact]
    public async Task AssignmentRequiresActiveParentAndAppropriateDepartment()
    {
        using var fixture = new Fixture();
        fixture.Workflow.Active = false;
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.Assign(fixture.Assignment()));
        fixture.Workflow.Active = true;
        fixture.SetUser(fixture.Draftsman);
        Assert.IsType<ForbidResult>(await fixture.Controller.Assign(fixture.Assignment()));
        Assert.Empty(await fixture.Db.DraftsmanTasks.ToListAsync());
    }

    [Theory]
    [InlineData("Architecture", "AD", "Architect Department \u2013 House Plan Design")]
    [InlineData("ThreeD", "3D", "Architecture Department \u2013 3D Visualization")]
    [InlineData("Structure", "SD", "Structure Department \u2013 Structural Design")]
    [InlineData("MEP", "MD", "MEP Department \u2013 MEP Design")]
    public async Task AssignmentWorksForEachSupportedParent(string parent, string departmentCode, string stepName)
    {
        using var fixture = new Fixture();
        fixture.Actor.Department!.DepartmentCode = departmentCode;
        fixture.Workflow.StepName = stepName;
        await fixture.Db.SaveChangesAsync();
        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.Assign(fixture.Assignment() with { ParentStep = parent }));
        Assert.Equal(parent, Assert.IsType<DraftsmanTaskResponse>(result.Value).ParentStep);
    }

    [Fact]
    public async Task PendingChildPreventsArchitectureApproval()
    {
        using var fixture = new Fixture();
        var plan = new ArchitecturalPlan { RequestId = fixture.Request.Id, FileUrl = "plan.pdf", UploadedBy = fixture.Actor.Id };
        fixture.Db.ArchitecturalPlans.Add(plan);
        await fixture.Controller.Assign(fixture.Assignment());
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateService();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApprovePlanAsync(fixture.Request.Id, plan.Id, fixture.Request.CustomerId));
        Assert.Contains("Drafts Man", error.Message);
    }

    [Theory]
    [InlineData("Structure")]
    [InlineData("ThreeD")]
    public async Task PendingChildPreventsParentCompletion(string parentStep)
    {
        using var fixture = new Fixture();
        fixture.Request.Status = PossessionRequestStatus.ThreeDDraftUploaded;
        fixture.Request.WorkflowRequestId = Guid.NewGuid();
        fixture.Db.DraftsmanTasks.Add(new DraftsmanTask
        {
            RequestId = fixture.Request.Id, ParentStep = parentStep, AssignedToUserId = fixture.Draftsman.Id
        });
        await fixture.Db.SaveChangesAsync();
        var service = new PossessionRequestService(fixture.Db,
            DispatchProxy.Create<IWorkflowEngine, ThreeDEngineProxy>(),
            DispatchProxy.Create<ITaskAssignmentService, SoilTestSchedulingTests.UnsupportedProxy>(),
            DispatchProxy.Create<INotificationService, SoilTestSchedulingTests.UnsupportedProxy>(),
            NullLogger<PossessionRequestService>.Instance);
        var error = parentStep == "Structure"
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteStructureAsync(fixture.Request.Id, "report.pdf", null, fixture.Actor.Id))
            : await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteThreeDAsync(fixture.Request.Id, fixture.Actor.Id));
        Assert.Contains("Drafts Man", error.Message);
    }

    [Fact]
    public async Task MepWaitsForAssignedChildInsteadOfLegacyDrawing()
    {
        using var fixture = new Fixture();
        fixture.Request.Status = PossessionRequestStatus.StructureCompleted;
        fixture.Request.StructureCompleted = true;
        fixture.Db.MEPReports.Add(new MEPReport { RequestId = fixture.Request.Id, FileUrl = "report.pdf" });
        fixture.Db.CadAssignments.Add(new CadAssignment { RequestId = fixture.Request.Id, CadType = "MEP", AssignedUserId = fixture.Draftsman.Id, FileUrl = "old.dwg", CompletedAt = DateTime.UtcNow });
        var task = new DraftsmanTask { RequestId = fixture.Request.Id, ParentStep = "MEP", AssignedToUserId = fixture.Draftsman.Id };
        fixture.Db.DraftsmanTasks.Add(task);
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateService();
        await service.TryCompleteMEPAsync(fixture.Request.Id, fixture.Actor.Id);
        Assert.False(fixture.Request.MEPCompleted);
        task.FileUrl = "new.dwg";
        task.CompletedAt = DateTime.UtcNow;
        await fixture.Db.SaveChangesAsync();
        await service.TryCompleteMEPAsync(fixture.Request.Id, fixture.Actor.Id);
        Assert.True(fixture.Request.MEPCompleted);
    }

    private sealed class Fixture : IDisposable
    {
        public DDFCDbContext Db { get; }
        public DraftsmanTasksController Controller { get; }
        public WorkflowProxy Workflow { get; }
        public User Actor { get; }
        public User Draftsman { get; }
        public PossessionRequest Request { get; }
        private readonly Department _draftDepartment;

        public Fixture()
        {
            Db = new DDFCDbContext(new DbContextOptionsBuilder<DDFCDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var architecture = new Department { DepartmentName = "Architecture Dept", DepartmentCode = "AD" };
            _draftDepartment = new Department { DepartmentName = "3D Dept", DepartmentCode = "3D" };
            Actor = new User { FullName = "Architect", Department = architecture };
            Draftsman = new User { FullName = "Draftsman", Department = _draftDepartment };
            var role = new Role("3D Operator");
            var customer = new Customer { FullName = "Customer", CNIC = "1234567890123", PhoneNumber = "123" };
            var plot = new Plot { PlotNumber = "1", SectorNo = "A", PhaseNo = "1" };
            Request = new PossessionRequest { RequestId = "DDFC-TEST", FileNo = "F1", Customer = customer, CustomerId = customer.Id, Plot = plot, PlotId = plot.Id };
            Db.AddRange(architecture, _draftDepartment, Actor, Draftsman, role, customer, plot, Request,
                new IdentityUserRole<Guid> { UserId = Draftsman.Id, RoleId = role.Id });
            Db.SaveChanges();
            var requests = DispatchProxy.Create<IPossessionRequestService, WorkflowProxy>();
            Workflow = (WorkflowProxy)requests;
            Controller = new DraftsmanTasksController(Db, requests)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            SetUser(Actor);
        }

        public User AddDraftsman()
        {
            var user = new User { FullName = "Other Draftsman", Department = _draftDepartment };
            Db.Users.Add(user);
            Db.SaveChanges();
            return user;
        }

        public void SetUser(User user) => Controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("userType", "staff"), new Claim("userId", user.Id.ToString())], "test"));
        public AssignDraftsmanRequest Assignment() => new() { RequestId = Request.Id, ParentStep = "Architecture", AssignedToUserId = Draftsman.Id };
        public PossessionRequestService CreateService() => new(Db,
            DispatchProxy.Create<IWorkflowEngine, SoilTestSchedulingTests.SoilEngineProxy>(),
            DispatchProxy.Create<ITaskAssignmentService, SoilTestSchedulingTests.UnsupportedProxy>(),
            DispatchProxy.Create<INotificationService, SoilTestSchedulingTests.UnsupportedProxy>(),
            NullLogger<PossessionRequestService>.Instance);
        public void Dispose() => Db.Dispose();
    }

    public class WorkflowProxy : DispatchProxy
    {
        public bool Active { get; set; } = true;
        public string StepName { get; set; } = "Architect Department \u2013 House Plan Design";
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name == nameof(IPossessionRequestService.GetActiveWorkflowStepNamesAsync)
            ? Task.FromResult<IEnumerable<string>>(Active ? [StepName] : [])
            : throw new NotSupportedException();
    }

    public class ThreeDEngineProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name == nameof(IWorkflowEngine.GetRequestAsync)
            ? Task.FromResult(new WorkflowEngine.Domain.Entities.Request
            {
                Steps = [new WorkflowEngine.Domain.Entities.RequestStep
                {
                    Status = WorkflowEngine.Domain.Enums.RequestStepStatus.Active,
                    ProcessStep = new WorkflowEngine.Domain.Entities.ProcessStep { Name = "Architecture Department \u2013 3D Visualization" }
                }]
            })
            : throw new NotSupportedException();
    }
}