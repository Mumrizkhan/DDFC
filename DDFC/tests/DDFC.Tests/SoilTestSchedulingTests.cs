using System.Reflection;
using System.Security.Claims;
using DDFC.API.Controllers;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using DDFC.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Enums;

namespace DDFC.Tests;

public class SoilTestSchedulingTests
{
    [Fact]
    public async Task ScheduleAndRescheduleNotifyCustomerAndRelevantStaff()
    {
        using var fixture = new Fixture();
        var first = await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input());
        Assert.IsType<OkObjectResult>(first);
        Assert.Single(await fixture.Db.SoilTestSchedules.ToListAsync());
        var customerNotification = Assert.Single(await fixture.Db.CustomerNotifications.ToListAsync());
        Assert.Equal(NotificationChannel.InApp, customerNotification.Channel);
        var staff = await fixture.Db.StaffNotifications.ToListAsync();
        Assert.Equal(2, staff.Count);
        Assert.Contains(staff, notification => notification.UserId == fixture.BcdUser.Id);
        Assert.Contains(staff, notification => notification.UserId == fixture.AdminUser.Id);
        Assert.DoesNotContain(staff, notification => notification.UserId == fixture.OtherUser.Id);
        Assert.Equal(3, fixture.Email.Addresses.Count);

        var later = fixture.Input() with { ScheduledAt = DateTimeOffset.UtcNow.AddDays(3) };
        Assert.IsType<OkObjectResult>(await fixture.Controller.Schedule(fixture.Request.Id, later));
        Assert.Single(await fixture.Db.SoilTestSchedules.ToListAsync());
        Assert.Equal(later.ScheduledAt, (await fixture.Db.SoilTestSchedules.SingleAsync()).ScheduledAt);
        Assert.Equal(2, await fixture.Db.CustomerNotifications.CountAsync());
        Assert.Equal(4, await fixture.Db.StaffNotifications.CountAsync());
    }

    [Fact]
    public async Task UnauthorizedStaffCannotSchedule()
    {
        using var fixture = new Fixture();
        fixture.SetUser(fixture.OtherUser, "Architect");
        Assert.IsType<ForbidResult>(await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input()));
        Assert.Empty(await fixture.Db.SoilTestSchedules.ToListAsync());
    }

    [Fact]
    public async Task InactiveLabOrInactiveStepCannotBeScheduled()
    {
        using var fixture = new Fixture();
        fixture.Lab.IsActive = false;
        await fixture.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input()));
        fixture.Lab.IsActive = true;
        fixture.Workflow.Active = false;
        await fixture.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input()));
        Assert.Empty(await fixture.Db.CustomerNotifications.ToListAsync());
    }

    [Fact]
    public async Task EmailFailureDoesNotLoseScheduleOrInAppNotifications()
    {
        using var fixture = new Fixture();
        fixture.Email.Fail = true;
        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input()));
        Assert.True((bool)result.Value!.GetType().GetProperty("emailWarning")!.GetValue(result.Value)!);
        Assert.Single(await fixture.Db.SoilTestSchedules.ToListAsync());
        Assert.Single(await fixture.Db.CustomerNotifications.ToListAsync());
        Assert.Equal(2, await fixture.Db.StaffNotifications.CountAsync());
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomersSchedule()
    {
        using var fixture = new Fixture();
        await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input());
        fixture.Controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("userType", "customer"), new Claim("customerId", Guid.NewGuid().ToString())
        ], "test"));
        Assert.IsType<NotFoundResult>(await fixture.Controller.Get(fixture.Request.Id));
    }

    [Fact]
    public async Task MissingDepartmentIsReported()
    {
        using var fixture = new Fixture();
        fixture.AdminUser.IsActive = false;
        await fixture.Db.SaveChangesAsync();
        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input()));
        Assert.True((bool)result.Value!.GetType().GetProperty("notificationWarning")!.GetValue(result.Value)!);
    }

    [Fact]
    public async Task SoilReportRequiresScheduleAndUsesConfiguredLab()
    {
        using var fixture = new Fixture();
        fixture.Request.WorkflowRequestId = Guid.NewGuid();
        fixture.Request.Status = PossessionRequestStatus.ArchitectAssigned;
        await fixture.Db.SaveChangesAsync();
        var engine = DispatchProxy.Create<IWorkflowEngine, SoilEngineProxy>();
        var service = new PossessionRequestService(fixture.Db, engine,
            DispatchProxy.Create<ITaskAssignmentService, UnsupportedProxy>(),
            DispatchProxy.Create<INotificationService, UnsupportedProxy>(),
            NullLogger<PossessionRequestService>.Instance);
        var report = new SoilTestDto(DateTime.UtcNow, "Untrusted lab", "100", "Suitable", "report.pdf");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitInitialSoilTestAsync(fixture.Request.Id, report, fixture.BcdUser.Id));
        await fixture.Controller.Schedule(fixture.Request.Id, fixture.Input());
        await service.SubmitInitialSoilTestAsync(fixture.Request.Id, report, fixture.BcdUser.Id);
        Assert.Equal(fixture.Lab.Name, (await fixture.Db.SoilTestReports.SingleAsync()).LabName);
        Assert.Equal(PossessionRequestStatus.ArchitectAssigned, fixture.Request.Status);
    }

    private sealed class Fixture : IDisposable
    {
        public DDFCDbContext Db { get; }
        public SoilTestSchedulesController Controller { get; }
        public SoilWorkflowProxy Workflow { get; }
        public RecordingEmail Email { get; } = new();
        public Laboratory Lab { get; } = new() { Name = "Test Lab" };
        public User BcdUser { get; }
        public User AdminUser { get; }
        public User OtherUser { get; }
        public PossessionRequest Request { get; }

        public Fixture()
        {
            Db = new DDFCDbContext(new DbContextOptionsBuilder<DDFCDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var bcd = new Department { DepartmentName = "Building Control", DepartmentCode = "BC" };
            var admin = new Department { DepartmentName = "Administration", DepartmentCode = "AS" };
            var other = new Department { DepartmentName = "Architecture", DepartmentCode = "AD" };
            BcdUser = new User { FullName = "BCD", Email = "bcd@example.com", Department = bcd };
            AdminUser = new User { FullName = "DDFC", Email = "ddfc@example.com", Department = admin };
            OtherUser = new User { FullName = "Architect", Email = "architect@example.com", Department = other };
            var customer = new Customer { FullName = "Customer", CNIC = "1234567890123", PhoneNumber = "123", Email = "customer@example.com" };
            var plot = new Plot { PlotNumber = "1", SectorNo = "A", PhaseNo = "1" };
            Request = new PossessionRequest { RequestId = "DDFC-TEST", FileNo = "F1", Customer = customer, CustomerId = customer.Id, Plot = plot, PlotId = plot.Id };
            Db.AddRange(bcd, admin, other, BcdUser, AdminUser, OtherUser, customer, plot, Request, Lab);
            Db.SaveChanges();
            var service = DispatchProxy.Create<IPossessionRequestService, SoilWorkflowProxy>();
            Workflow = (SoilWorkflowProxy)service;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Smtp:Host"] = "test" }).Build();
            Controller = new SoilTestSchedulesController(Db, service, Email, config, NullLogger<SoilTestSchedulesController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            SetUser(BcdUser, "Building Control Officer");
        }

        public void SetUser(User user, string role) => Controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("userType", "staff"), new Claim("userId", user.Id.ToString()), new Claim("role", role)], "test"));
        public ScheduleSoilTestRequest Input() => new() { LaboratoryId = Lab.Id, ScheduledAt = DateTimeOffset.UtcNow.AddDays(1), Location = "Plot" };
        public void Dispose() => Db.Dispose();
    }

    public class SoilWorkflowProxy : DispatchProxy
    {
        public bool Active { get; set; } = true;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name == nameof(IPossessionRequestService.GetActiveWorkflowStepNamesAsync)
            ? Task.FromResult<IEnumerable<string>>(Active ? ["Soil Test"] : [])
            : throw new NotSupportedException();
    }

    public class SoilEngineProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            nameof(IWorkflowEngine.GetRequestAsync) => Task.FromResult(new WorkflowEngine.Domain.Entities.Request
            {
                Steps = [new WorkflowEngine.Domain.Entities.RequestStep
                {
                    Status = RequestStepStatus.Active,
                    ProcessStep = new WorkflowEngine.Domain.Entities.ProcessStep { Name = "Soil Test" }
                }]
            }),
            nameof(IWorkflowEngine.CompleteStepAsync) => Task.CompletedTask,
            _ => throw new NotSupportedException()
        };
    }

    public class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException();
    }

    public sealed class RecordingEmail : IEmailService
    {
        public List<string> Addresses { get; } = [];
        public bool Fail { get; set; }
        public Task SendAsync(string to, string subject, string htmlBody)
        {
            if (Fail) throw new InvalidOperationException("SMTP unavailable");
            Addresses.Add(to);
            return Task.CompletedTask;
        }
    }
}