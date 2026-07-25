using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize(Policy = "StaffOrAdmin")]
public class TasksController : ControllerBase
{
    private readonly ITaskAssignmentService _svc;
    private readonly DDFCDbContext          _db;

    public TasksController(ITaskAssignmentService svc, DDFCDbContext db)
    {
        _svc = svc;
        _db  = db;
    }

    // ── My Tasks ─────────────────────────────────────────────────────────────
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = GetStaffId();
        var tasks  = await _svc.GetPendingTasksForUserAsync(userId);
        return Ok(tasks);
    }

    [HttpGet("request/{requestId:guid}")]
    public async Task<IActionResult> GetForRequest(Guid requestId)
        => Ok(await _svc.GetTasksForRequestAsync(requestId));

    // ── Department Scoped ─────────────────────────────────────────────────────
    /// <summary>All tasks (any status) in a department. Managers only.</summary>
    [HttpGet("department/{deptId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetDepartmentTasks(Guid deptId)
    {
        var tasks = await _db.TaskAssignments
            .Include(t => t.Request)
            .Include(t => t.AssignedToUser)
            .Where(t => t.DepartmentId == deptId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return Ok(tasks);
    }

    /// <summary>Tasks with no assignee in a department.</summary>
    [HttpGet("department/{deptId:guid}/unassigned")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUnassignedTasks(Guid deptId)
    {
        var tasks = await _db.TaskAssignments
            .Include(t => t.Request)
            .Where(t => t.DepartmentId == deptId && t.AssignedToUserId == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
        return Ok(tasks);
    }

    /// <summary>Per-employee workload stats for a department.</summary>
    [HttpGet("department/{deptId:guid}/workload")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetDepartmentWorkload(Guid deptId)
    {
        var users = await _db.Users
            .Where(u => u.DepartmentId == deptId && u.IsActive)
            .ToListAsync();

        var tasks = await _db.TaskAssignments
            .Where(t => t.DepartmentId == deptId)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        var result = users.Select(u => new
        {
            userId         = u.Id,
            employeeName   = u.FullName,
            assigned       = tasks.Count(t => t.AssignedToUserId == u.Id && t.Status == TaskAssignmentStatus.Pending),
            inProgress     = tasks.Count(t => t.AssignedToUserId == u.Id && t.Status == TaskAssignmentStatus.InProgress),
            completedToday = tasks.Count(t => t.AssignedToUserId == u.Id && t.Status == TaskAssignmentStatus.Completed
                                           && t.UpdatedAt.HasValue && t.UpdatedAt.Value.Date == today),
            isAvailable    = u.IsAvailable,
            avgCompletionTime = (double?)null  // can be computed later if needed
        }).ToList();

        return Ok(result);
    }

    // ── Manual Assign ─────────────────────────────────────────────────────────
    /// <summary>Manually assign a task to a specific employee.</summary>
    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignDto dto)
    {
        try
        {
            var task = await _svc.ReassignAsync(id, dto.EmployeeId, GetStaffId(), dto.Reason ?? "Manual assignment");
            return Ok(task);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Reassign (Admin) ──────────────────────────────────────────────────────
    [HttpPost("{id:guid}/reassign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reassign(Guid id, [FromBody] ReassignDto dto)
    {
        try
        {
            var task = await _svc.ReassignAsync(id, dto.NewUserId, GetStaffId(), dto.Reason);
            return Ok(task);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Complete ──────────────────────────────────────────────────────────────
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            await _svc.CompleteTaskAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Round-Robin ───────────────────────────────────────────────────────────
    /// <summary>Returns current round-robin state for a department (index + next user).</summary>
    [HttpGet("department/{deptId:guid}/round-robin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetRoundRobinState(Guid deptId)
    {
        var dept = await _db.Departments
            .Include(d => d.Users.Where(u => u.IsActive))
            .FirstOrDefaultAsync(d => d.Id == deptId);

        if (dept is null) return NotFound();

        var available = dept.Users.Where(u => u.IsAvailable).OrderBy(u => u.FullName).ToList();
        var nextUser = available.Count > 0
            ? available[dept.LastAssignedEmployeeIndex % available.Count]
            : null;

        return Ok(new
        {
            departmentId          = dept.Id,
            departmentName        = dept.DepartmentName,
            lastAssignedIndex     = dept.LastAssignedEmployeeIndex,
            availableCount        = available.Count,
            nextUser              = nextUser is null ? null : new { nextUser.Id, nextUser.FullName }
        });
    }

    /// <summary>Resets the round-robin index to 0 for a department.</summary>
    [HttpPost("department/{deptId:guid}/round-robin/reset")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ResetRoundRobin(Guid deptId)
    {
        var dept = await _db.Departments.FindAsync(deptId);
        if (dept is null) return NotFound();

        dept.LastAssignedEmployeeIndex = 0;
        _db.Departments.Update(dept);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Employee Availability ─────────────────────────────────────────────────
    [HttpPut("/api/v1/employees/{userId:guid}/availability")]
    public async Task<IActionResult> SetAvailability(Guid userId, [FromBody] AvailabilityDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.IsAvailable = dto.IsAvailable;
        _db.Users.Update(user);

        // Also log to EmployeeAvailability table for history
        var avail = new EmployeeAvailability
        {
            UserId       = userId,
            DepartmentId = user.DepartmentId ?? Guid.Empty,
            IsAvailable  = dto.IsAvailable,
            Reason       = dto.Reason,
            SetByUserId  = GetStaffId(),
            UnavailableFrom  = dto.IsAvailable ? null : DateTime.UtcNow,
            UnavailableUntil = dto.UnavailableUntil,
        };
        _db.EmployeeAvailabilities.Add(avail);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetStaffId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

public record ReassignDto(Guid NewUserId, string Reason);
public record AssignDto(Guid EmployeeId, string? Reason);
public record AvailabilityDto(bool IsAvailable, string? Reason, DateTime? UnavailableUntil);
