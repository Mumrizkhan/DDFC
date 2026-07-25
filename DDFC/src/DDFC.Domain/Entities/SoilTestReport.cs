using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class SoilTestReport : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public DateTime TestDate { get; set; }
    public string LabName { get; set; } = string.Empty;
    public string SoilBearingCapacity { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string ReportFileUrl { get; set; } = string.Empty;  // Mandatory PDF
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
