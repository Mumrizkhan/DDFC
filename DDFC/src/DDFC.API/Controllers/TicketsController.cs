using DDFC.Application.Interfaces;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService  _svc;
    private readonly DDFCDbContext   _db;
    public TicketsController(ITicketService svc, DDFCDbContext db) { _svc = svc; _db = db; }

    [HttpPost]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
    {
        var customerId = GetCustomerId();
        var ticket = await _svc.CreateTicketAsync(customerId, dto.RequestId,
            dto.Subject, dto.Category, dto.Description);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpGet("my")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetMine()
        => Ok(await _svc.GetTicketsForCustomerAsync(GetCustomerId()));

    [HttpGet("queue")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> GetQueue()
    {
        var deptId = GetDepartmentId();
        return Ok(await _svc.GetTicketsForDepartmentAsync(deptId));
    }

    [HttpGet("assigned-to-me")]
    [Authorize(Policy = "CanReplyTickets")]
    public async Task<IActionResult> GetAssignedToMe()
        => Ok(await _svc.GetTicketsForUserAsync(GetStaffId()));

    [HttpGet("department/{deptId:guid}")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetForDepartment(Guid deptId)
        => Ok(await _svc.GetTicketsForDepartmentAsync(deptId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _svc.GetTicketAsync(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAll()
        => Ok(await _svc.GetAllTicketsAsync());

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyDto dto)
    {
        var isCustomer = User.HasClaim("userType", "customer");
        var authorId   = isCustomer ? GetCustomerId() : GetStaffId();
        var authorType = isCustomer ? AuthorType.Customer : AuthorType.Staff;
        try
        {
            var reply = await _svc.ReplyAsync(id, authorId, authorType, dto.Message, dto.AttachmentUrl);
            return Ok(reply);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseTicketDto? dto)
    {
        try
        {
            var t = await _svc.CloseTicketAsync(id, dto?.SatisfactionRating);
            return Ok(t);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketDto dto)
    {
        var t = await _svc.AssignTicketAsync(id, dto.DepartmentId);
        return Ok(t);
    }

    // ── Technical Support Endpoints ────────────────────────────────────────────

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "CanResolveTickets")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        try
        {
            var t = await _svc.ResolveTicketAsync(id, GetStaffId());
            return Ok(t);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "CanReopenTickets")]
    public async Task<IActionResult> Reopen(Guid id)
    {
        try
        {
            var t = await _svc.ReopenTicketAsync(id);
            return Ok(t);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id:guid}/assign-user")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> AssignToUser(Guid id, [FromBody] AssignUserDto dto)
    {
        try
        {
            var t = await _svc.AssignToUserAsync(id, dto.UserId);
            return Ok(t);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private Guid GetCustomerId()  => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private Guid GetStaffId()     => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private Guid GetDepartmentId()=> Guid.Parse(User.FindFirst("departmentId")!.Value);
}

public record CreateTicketDto(Guid? RequestId, string Subject, TicketCategory Category, string Description);
public record ReplyDto(string Message, string? AttachmentUrl);
public record CloseTicketDto(int? SatisfactionRating);
public record AssignTicketDto(Guid DepartmentId);
public record AssignUserDto(Guid UserId);
