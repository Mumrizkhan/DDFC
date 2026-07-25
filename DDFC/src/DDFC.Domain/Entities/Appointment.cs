using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class Appointment : BaseEntity
{
    // Customer attending
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Optional link to a specific possession request
    public Guid? RequestId { get; set; }
    public PossessionRequest? Request { get; set; }

    // Target department
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // Assigned employee (may be set at booking time or later)
    public Guid? AssignedEmployeeId { get; set; }
    public User? AssignedEmployee { get; set; }

    // Appointment schedule
    public DateTime AppointmentDate { get; set; }
    /// <summary>Start time of day (e.g., 09:00).</summary>
    public TimeSpan StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;

    // Client attendees — max 2
    public int NumberOfPersons { get; set; } = 1;
    /// <summary>Comma-separated names of client attendees.</summary>
    public string? AttendeeNames { get; set; }

    // How the appointment was booked
    public AppointmentBookingMethod BookingMethod { get; set; }

    // Booked by (receptionist or any staff member)
    public Guid BookedByUserId { get; set; }
    public User BookedByUser { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? Purpose { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
}
