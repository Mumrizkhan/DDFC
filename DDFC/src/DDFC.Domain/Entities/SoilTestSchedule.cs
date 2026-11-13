using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class SoilTestSchedule : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public Guid LaboratoryId { get; set; }
    public Laboratory Laboratory { get; set; } = null!;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid ScheduledByUserId { get; set; }
}