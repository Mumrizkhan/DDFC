using System.ComponentModel.DataAnnotations;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/draftsman-tasks")]
[Authorize(Policy = "StaffOrAdmin")]
public class DraftsmanTasksController(DDFCDbContext db, IPossessionRequestService requests) : ControllerBase
{
    private static readonly Dictionary<string, (string StepName, string DepartmentCode)> Parents = new(StringComparer.Ordinal)
    {
        ["Architecture"] = ("Architect Department \u2013 House Plan Design", "AD"),
        ["ThreeD"] = ("Architecture Department \u2013 3D Visualization", "3D"),
        ["Structure"] = ("Structure Department \u2013 Structural Design", "SD"),
        ["MEP"] = ("MEP Department \u2013 MEP Design", "MD")
    };

    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetUserId(out var userId)) return Forbid();
        var tasks = await db.DraftsmanTasks.Include(task => task.Request).Include(task => task.AssignedToUser)
            .Where(task => task.AssignedToUserId == userId).OrderByDescending(task => task.AssignedAt).ToListAsync();
        return Ok(tasks.Select(ToResponse));
    }

    [HttpGet("request/{requestId:guid}")]
    public async Task<IActionResult> GetForRequest(Guid requestId)
    {
        var tasks = await db.DraftsmanTasks.Include(task => task.Request).Include(task => task.AssignedToUser)
            .Where(task => task.RequestId == requestId).ToListAsync();
        return Ok(tasks.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!TryGetUserId(out var userId)) return Forbid();
        var task = await db.DraftsmanTasks.Include(item => item.Request).Include(item => item.AssignedToUser)
            .FirstOrDefaultAsync(item => item.Id == id && item.AssignedToUserId == userId);
        return task == null ? NotFound() : Ok(ToResponse(task));
    }

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignDraftsmanRequest input)
    {
        if (!TryGetUserId(out var userId)) return Forbid();
        if (!Parents.TryGetValue(input.ParentStep, out var parent))
            return BadRequest(new { message = "Invalid parent step." });
        var actor = await db.Users.Include(user => user.Department).FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        if (actor == null || (!User.HasClaim("role", "Admin") && actor.Department?.DepartmentCode != parent.DepartmentCode))
            return Forbid();
        var request = await db.PossessionRequests.FirstOrDefaultAsync(item => item.Id == input.RequestId);
        if (request == null) return NotFound();
        if (!(await requests.GetActiveWorkflowStepNamesAsync(input.RequestId)).Contains(parent.StepName))
            return BadRequest(new { message = "The parent workflow step is not active." });
        var assignee = await db.Users.Include(user => user.Department)
            .FirstOrDefaultAsync(user => user.Id == input.AssignedToUserId && user.IsActive);
        if (assignee == null) return BadRequest(new { message = "Select an active draftsman." });
        var isOperator = await db.UserRoles.AnyAsync(membership => membership.UserId == assignee.Id &&
            db.Roles.Any(role => role.Id == membership.RoleId && role.Name == "3D Operator"));
        if (!isOperator && !(assignee.Department?.DepartmentName.Contains("3D", StringComparison.OrdinalIgnoreCase) == true ||
            assignee.Department?.DepartmentName.Contains("three d", StringComparison.OrdinalIgnoreCase) == true))
            return BadRequest(new { message = "Select an active draftsman." });

        var task = await db.DraftsmanTasks.FirstOrDefaultAsync(item => item.RequestId == input.RequestId && item.ParentStep == input.ParentStep);
        if (task == null)
        {
            task = new DraftsmanTask { RequestId = input.RequestId, ParentStep = input.ParentStep };
            db.DraftsmanTasks.Add(task);
        }
        task.Request = request;
        task.AssignedToUser = assignee;
        task.AssignedToUserId = assignee.Id;
        task.AssignedByUserId = userId;
        task.AssignedAt = DateTime.UtcNow;
        task.FileUrl = null;
        task.CompletedAt = null;
        await db.SaveChangesAsync();
        return Ok(ToResponse(task));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteDraftsmanRequest input)
    {
        if (!TryGetUserId(out var userId)) return Forbid();
        var task = await db.DraftsmanTasks.Include(item => item.Request).Include(item => item.AssignedToUser)
            .FirstOrDefaultAsync(item => item.Id == id && item.AssignedToUserId == userId);
        if (task == null) return NotFound();
        if (task.CompletedAt.HasValue) return Conflict(new { message = "This child step is already complete." });
        if (string.IsNullOrWhiteSpace(input.FileUrl)) return BadRequest(new { message = "A drawing must be attached." });
        if (!Parents.TryGetValue(task.ParentStep, out var parent) ||
            !(await requests.GetActiveWorkflowStepNamesAsync(task.RequestId)).Contains(parent.StepName))
            return BadRequest(new { message = "The parent workflow step is not active." });

        task.FileUrl = input.FileUrl.Trim();
        task.CompletedAt = DateTime.UtcNow;
        db.Documents.Add(new Document
        {
            RequestId = task.RequestId, StepName = parent.StepName,
            DocType = $"{task.ParentStep} Drafts Man Document", FileUrl = task.FileUrl,
            UploadedBy = userId, UploadedAt = task.CompletedAt.Value
        });
        await db.SaveChangesAsync();
        if (task.ParentStep == "MEP")
            await requests.TryCompleteMEPAsync(task.RequestId, userId);
        return Ok(ToResponse(task));
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("userId")?.Value, out userId);

    private static DraftsmanTaskResponse ToResponse(DraftsmanTask task) => new(
        task.Id, task.RequestId, task.Request.RequestId, task.ParentStep, task.AssignedToUserId,
        task.AssignedToUser.FullName, task.AssignedAt, task.FileUrl, task.CompletedAt);
}

/// <summary>Assigns a draftsman child step to a user.</summary>
public sealed record AssignDraftsmanRequest
{
    [Required] public required Guid RequestId { get; init; }
    [Required, MaxLength(100)] public required string ParentStep { get; init; }
    [Required] public required Guid AssignedToUserId { get; init; }
}

/// <summary>Completes a draftsman child step with its drawing.</summary>
public sealed record CompleteDraftsmanRequest
{
    [Required] public required string FileUrl { get; init; }
}

/// <summary>A draftsman child step and its parent request.</summary>
public sealed record DraftsmanTaskResponse(Guid Id, Guid RequestId, string RequestNumber, string ParentStep,
    Guid AssignedToUserId, string AssignedToUserName, DateTime AssignedAt, string? FileUrl, DateTime? CompletedAt);