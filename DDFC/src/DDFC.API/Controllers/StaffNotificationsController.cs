using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/staff-notifications")]
[Authorize(Policy = "StaffOrAdmin")]
public class StaffNotificationsController(DDFCDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (!Guid.TryParse(User.FindFirst("userId")?.Value, out var userId)) return Forbid();
        var notifications = await db.StaffNotifications.Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SentAt).Take(100)
            .Select(item => new { item.Id, item.RequestId, item.Title, item.MessageBody, item.SentAt, item.IsRead })
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        if (!Guid.TryParse(User.FindFirst("userId")?.Value, out var userId)) return Forbid();
        var notification = await db.StaffNotifications.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (notification == null) return NotFound();
        notification.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}