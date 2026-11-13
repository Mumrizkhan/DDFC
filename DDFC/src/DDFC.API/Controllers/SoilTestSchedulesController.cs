using System.ComponentModel.DataAnnotations;
using System.Net;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/requests/{requestId:guid}/soil-test/schedule")]
[Authorize]
public class SoilTestSchedulesController(
    DDFCDbContext db, IPossessionRequestService requests, IEmailService email,
    IConfiguration configuration, ILogger<SoilTestSchedulesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid requestId)
    {
        if (User.HasClaim("userType", "customer"))
        {
            if (!Guid.TryParse(User.FindFirst("customerId")?.Value, out var customerId) ||
                !await db.PossessionRequests.AnyAsync(request => request.Id == requestId && request.CustomerId == customerId))
                return NotFound();
        }
        else if (!User.HasClaim("userType", "staff")) return Forbid();

        var schedule = await db.SoilTestSchedules.Include(item => item.Laboratory)
            .FirstOrDefaultAsync(item => item.RequestId == requestId);
        return schedule == null ? NoContent() : Ok(ToResponse(schedule));
    }

    [HttpPut]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Schedule(Guid requestId, [FromBody] ScheduleSoilTestRequest input)
    {
        if (!Guid.TryParse(User.FindFirst("userId")?.Value, out var userId)) return Forbid();
        var actor = await db.Users.Include(user => user.Department).FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        if (actor == null || !(actor.Department?.DepartmentCode is "AS" or "BC" ||
            User.HasClaim("role", "Admin") || User.HasClaim("role", "DDFC Admin") || User.HasClaim("role", "Possession Admin")))
            return Forbid();
        if (input.ScheduledAt <= DateTimeOffset.UtcNow)
            return BadRequest(new { message = "The scheduled time must be in the future." });
        var request = await db.PossessionRequests.Include(item => item.Customer).FirstOrDefaultAsync(item => item.Id == requestId);
        if (request == null) return NotFound();
        if (!(await requests.GetActiveWorkflowStepNamesAsync(requestId)).Any(name => name == "Soil Test"))
            return BadRequest(new { message = "The Soil Test step is not active." });
        var lab = await db.Laboratories.FirstOrDefaultAsync(item => item.Id == input.LaboratoryId && item.IsActive);
        if (lab == null) return BadRequest(new { message = "Select an active lab." });

        var schedule = await db.SoilTestSchedules.FirstOrDefaultAsync(item => item.RequestId == requestId);
        if (schedule == null)
        {
            schedule = new SoilTestSchedule { RequestId = requestId };
            db.SoilTestSchedules.Add(schedule);
        }
        schedule.LaboratoryId = lab.Id;
        schedule.Laboratory = lab;
        schedule.ScheduledAt = input.ScheduledAt;
        schedule.Location = input.Location?.Trim();
        schedule.Notes = input.Notes?.Trim();
        schedule.ScheduledByUserId = userId;

        var title = "Soil test scheduled";
        var body = $"Soil test for {request.RequestId} is scheduled with {lab.Name} on {input.ScheduledAt:dd MMM yyyy HH:mm zzz}.";
        if (!string.IsNullOrWhiteSpace(schedule.Location)) body += $" Location: {schedule.Location}.";
        if (!string.IsNullOrWhiteSpace(schedule.Notes)) body += $" Notes: {schedule.Notes}.";

        var recipientIds = await db.UserRoles.Join(db.Roles, membership => membership.RoleId, role => role.Id,
                (membership, role) => new { membership.UserId, role.Name })
            .Where(item => item.Name == "Admin" || item.Name == "DDFC Admin" || item.Name == "Possession Admin")
            .Select(item => item.UserId).Distinct().ToListAsync();
        var recipients = await db.Users.Include(user => user.Department)
            .Where(user => user.IsActive && ((user.Department != null &&
                (user.Department.DepartmentCode == "AS" || user.Department.DepartmentCode == "BC")) || recipientIds.Contains(user.Id)))
            .ToListAsync();
        db.CustomerNotifications.Add(new CustomerNotification
        {
            CustomerId = request.CustomerId, RequestId = requestId, Title = title, MessageBody = body,
            Channel = NotificationChannel.InApp
        });
        db.StaffNotifications.AddRange(recipients.Select(user => new StaffNotification
        {
            UserId = user.Id, RequestId = requestId, Title = title, MessageBody = body
        }));
        await db.SaveChangesAsync();

        var notificationWarning = !recipients.Any(user => user.Department?.DepartmentCode == "BC") ||
            !recipients.Any(user => user.Department?.DepartmentCode == "AS" || recipientIds.Contains(user.Id));
        var smtpConfigured = !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]);
        var emailWarning = !smtpConfigured || string.IsNullOrWhiteSpace(request.Customer.Email) ||
            recipients.Any(user => string.IsNullOrWhiteSpace(user.Email));
        if (smtpConfigured)
        {
            var addresses = recipients.Select(user => user.Email).Append(request.Customer.Email)
                .Where(address => !string.IsNullOrWhiteSpace(address)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var address in addresses)
            {
                try { await email.SendAsync(address!, title, $"<p>{WebUtility.HtmlEncode(body)}</p>"); }
                catch (Exception exception)
                {
                    emailWarning = true;
                    logger.LogWarning(exception, "Unable to email soil-test schedule for request {RequestId}", requestId);
                }
            }
        }
        return Ok(new { schedule = ToResponse(schedule), notificationWarning, emailWarning });
    }

    private static SoilTestScheduleResponse ToResponse(SoilTestSchedule schedule) =>
        new(schedule.LaboratoryId, schedule.Laboratory.Name, schedule.ScheduledAt, schedule.Location, schedule.Notes);
}

/// <summary>Lab-provided date and location for a soil test.</summary>
public sealed record ScheduleSoilTestRequest
{
    [Required] public required Guid LaboratoryId { get; init; }
    [Required] public required DateTimeOffset ScheduledAt { get; init; }
    [MaxLength(500)] public string? Location { get; init; }
    [MaxLength(2000)] public string? Notes { get; init; }
}

/// <summary>Scheduled soil test and its laboratory.</summary>
public sealed record SoilTestScheduleResponse(Guid LaboratoryId, string LaboratoryName, DateTimeOffset ScheduledAt, string? Location, string? Notes);