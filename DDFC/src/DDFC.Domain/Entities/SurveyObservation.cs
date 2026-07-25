using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class SurveyObservation : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string OfficerName { get; set; } = string.Empty;
    public DateTime SurveyDate { get; set; }
    public string? Observations { get; set; }
    public string? Violations { get; set; }
    public string? Suggestions { get; set; }
    public string? PhotoUrls { get; set; }   // JSON array of URLs
}
