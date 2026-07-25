using DDFC.Application.Interfaces;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize(Policy = "CustomerOnly")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _svc;
    private readonly DDFCDbContext        _db;

    public NotificationsController(INotificationService svc, DDFCDbContext db)
    {
        _svc = svc;
        _db  = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool? unreadOnly)
    {
        var cid = GetCustomerId();
        var q = _db.CustomerNotifications
            .Where(n => n.CustomerId == cid);
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);
        var list = await q.OrderByDescending(n => n.SentAt).ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var ok = await _svc.MarkReadAsync(id, GetCustomerId());
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondDto dto)
    {
        var ok = await _svc.SubmitResponseAsync(id, GetCustomerId(), dto.ResponseText);
        return ok ? NoContent() : BadRequest(new { message = "Notification not found or does not require a response." });
    }

    private Guid GetCustomerId() => Guid.Parse(User.FindFirst("customerId")!.Value);
}

public record RespondDto(string ResponseText);
