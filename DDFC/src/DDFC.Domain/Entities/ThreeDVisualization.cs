using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class ThreeDVisualization : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string FileUrl  { get; set; } = string.Empty;
    /// <summary>skp | jpeg | jpg | png</summary>
    public string FileType { get; set; } = string.Empty;

    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
