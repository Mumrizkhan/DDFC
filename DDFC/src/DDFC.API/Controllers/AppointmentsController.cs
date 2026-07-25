using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly DDFCDbContext _db;
    public AppointmentsController(DDFCDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid?   departmentId,
        [FromQuery] string? status,
        [FromQuery] string? date)
    {
        var q = _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Department)
            .Include(a => a.AssignedEmployee)
            .Include(a => a.BookedByUser)
            .Include(a => a.Request)
            .AsQueryable();

        if (departmentId.HasValue)
            q = q.Where(a => a.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
            q = q.Where(a => a.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
            q = q.Where(a => a.AppointmentDate.Date == parsedDate.ToDateTime(TimeOnly.MinValue).Date);

        var list = await q.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var a = await FindAsync(id);
        return a is null ? NotFound() : Ok(MapToDto(a));
    }

    // ── Availability: free employees in a dept on a given date/time ──────────
    [HttpGet("availability")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid   departmentId,
        [FromQuery] string date,
        [FromQuery] string startTime,
        [FromQuery] int    durationMinutes = 30)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(new { message = "Invalid date format (yyyy-MM-dd expected)" });
        if (!TimeSpan.TryParse(startTime, out var st))
            return BadRequest(new { message = "Invalid time format (HH:mm expected)" });

        var slotStart = d.ToDateTime(TimeOnly.FromTimeSpan(st));
        var slotEnd   = slotStart.AddMinutes(durationMinutes);

        // Employees in the department
        var employees = await _db.Users
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync();

        // Candidate appointments for that department/day (SQL-translatable filters only)
        var sameDayAppointments = await _db.Appointments
            .Where(a =>
                a.DepartmentId == departmentId &&
                a.Status == AppointmentStatus.Scheduled &&
                a.AppointmentDate.Date == slotStart.Date &&
                a.AssignedEmployeeId.HasValue)
            .Select(a => new { a.AssignedEmployeeId, a.AppointmentDate, a.StartTime, a.DurationMinutes })
            .ToListAsync();

        // Conflicting appointments at that slot (overlap evaluated in memory)
        var busyEmployeeIds = sameDayAppointments
            .Where(a =>
                // overlaps: starts before slotEnd AND ends after slotStart
                a.AppointmentDate.Add(a.StartTime) < slotEnd &&
                a.AppointmentDate.Add(a.StartTime).AddMinutes(a.DurationMinutes) > slotStart)
            .Select(a => a.AssignedEmployeeId!.Value)
            .ToList();

        var available = employees
            .Where(e => !busyEmployeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FullName, e.Email })
            .ToList();

        return Ok(available);
    }

    // ── Book ──────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Book([FromBody] BookAppointmentDto dto)
    {
        if (dto.NumberOfPersons is < 1 or > 2)
            return BadRequest(new { message = "Only 1 or 2 persons are allowed per appointment." });

        if (!DateOnly.TryParse(dto.AppointmentDate, out var d))
            return BadRequest(new { message = "Invalid date format (yyyy-MM-dd expected)" });
        if (!TimeSpan.TryParse(dto.StartTime, out var st))
            return BadRequest(new { message = "Invalid time format (HH:mm expected)" });

        // Validate employee availability if one is assigned
        if (dto.AssignedEmployeeId.HasValue)
        {
            var slotStart = d.ToDateTime(TimeOnly.FromTimeSpan(st));
            var slotEnd   = slotStart.AddMinutes(dto.DurationMinutes);

            // Fetch candidate appointments with SQL-translatable filters, then check overlap in memory
            var sameDayAppointments = await _db.Appointments
                .Where(a =>
                    a.AssignedEmployeeId == dto.AssignedEmployeeId &&
                    a.Status == AppointmentStatus.Scheduled &&
                    a.AppointmentDate.Date == slotStart.Date)
                .Select(a => new { a.AppointmentDate, a.StartTime, a.DurationMinutes })
                .ToListAsync();

            var conflict = sameDayAppointments.Any(a =>
                a.AppointmentDate.Add(a.StartTime) < slotEnd &&
                a.AppointmentDate.Add(a.StartTime).AddMinutes(a.DurationMinutes) > slotStart);

            if (conflict)
                return Conflict(new { message = "The selected employee already has an appointment at this time." });
        }

        var appointment = new Appointment
        {
            CustomerId          = dto.CustomerId,
            RequestId           = dto.RequestId,
            DepartmentId        = dto.DepartmentId,
            AssignedEmployeeId  = dto.AssignedEmployeeId,
            AppointmentDate     = d.ToDateTime(TimeOnly.MinValue),
            StartTime           = st,
            DurationMinutes     = dto.DurationMinutes,
            NumberOfPersons     = dto.NumberOfPersons,
            AttendeeNames       = dto.AttendeeNames,
            BookingMethod       = dto.BookingMethod,
            BookedByUserId      = GetCurrentUserId(),
            Purpose             = dto.Purpose,
            Notes               = dto.Notes,
            Status              = AppointmentStatus.Scheduled,
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var created = await FindAsync(appointment.Id);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, MapToDto(created!));
    }

    // ── Status updates ─────────────────────────────────────────────────────────
    [HttpPatch("{id:guid}/complete")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] AppointmentNoteDto dto)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a is null) return NotFound();
        a.Status = AppointmentStatus.Completed;
        if (!string.IsNullOrWhiteSpace(dto.Notes)) a.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] AppointmentNoteDto dto)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a is null) return NotFound();
        a.Status = AppointmentStatus.Cancelled;
        a.CancellationReason = dto.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/no-show")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> NoShow(Guid id)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a is null) return NotFound();
        a.Status = AppointmentStatus.NoShow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Assign employee (can be done after booking) ────────────────────────────
    [HttpPatch("{id:guid}/assign")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignEmployeeDto dto)
    {
        var a = await _db.Appointments.FindAsync(id);
        if (a is null) return NotFound();
        a.AssignedEmployeeId = dto.EmployeeId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Task<Appointment?> FindAsync(Guid id) =>
        _db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Department)
            .Include(a => a.AssignedEmployee)
            .Include(a => a.BookedByUser)
            .Include(a => a.Request)
            .FirstOrDefaultAsync(a => a.Id == id);

    private static AppointmentDto MapToDto(Appointment a) => new(
        Id:                 a.Id,
        CustomerId:         a.CustomerId,
        CustomerName:       a.Customer?.FullName,
        CustomerPhone:      a.Customer?.PhoneNumber,
        RequestId:          a.RequestId,
        RequestCode:        a.Request?.RequestId,
        DepartmentId:       a.DepartmentId,
        DepartmentName:     a.Department?.DepartmentName,
        AssignedEmployeeId: a.AssignedEmployeeId,
        AssignedEmployeeName: a.AssignedEmployee?.FullName,
        AppointmentDate:    a.AppointmentDate.ToString("yyyy-MM-dd"),
        StartTime:          a.StartTime.ToString(@"hh\:mm"),
        DurationMinutes:    a.DurationMinutes,
        NumberOfPersons:    a.NumberOfPersons,
        AttendeeNames:      a.AttendeeNames,
        BookingMethod:      a.BookingMethod.ToString(),
        BookedByName:       a.BookedByUser?.FullName,
        Status:             a.Status.ToString(),
        Purpose:            a.Purpose,
        Notes:              a.Notes,
        CancellationReason: a.CancellationReason,
        CreatedAt:          a.CreatedAt
    );

    private Guid GetCurrentUserId()
    {
        var v = User.FindFirst("userId")?.Value
            ?? throw new InvalidOperationException("No user claim");
        return Guid.Parse(v);
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record BookAppointmentDto(
    Guid                      CustomerId,
    Guid?                     RequestId,
    Guid                      DepartmentId,
    Guid?                     AssignedEmployeeId,
    string                    AppointmentDate,
    string                    StartTime,
    int                       DurationMinutes,
    int                       NumberOfPersons,
    string?                   AttendeeNames,
    AppointmentBookingMethod  BookingMethod,
    string?                   Purpose,
    string?                   Notes);

public record AppointmentNoteDto(string? Notes);
public record AssignEmployeeDto(Guid? EmployeeId);

public record AppointmentDto(
    Guid      Id,
    Guid      CustomerId,
    string?   CustomerName,
    string?   CustomerPhone,
    Guid?     RequestId,
    string?   RequestCode,
    Guid      DepartmentId,
    string?   DepartmentName,
    Guid?     AssignedEmployeeId,
    string?   AssignedEmployeeName,
    string    AppointmentDate,
    string    StartTime,
    int       DurationMinutes,
    int       NumberOfPersons,
    string?   AttendeeNames,
    string    BookingMethod,
    string?   BookedByName,
    string    Status,
    string?   Purpose,
    string?   Notes,
    string?   CancellationReason,
    DateTime  CreatedAt);
