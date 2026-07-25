using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

/// <summary>
/// Records the merging of an adjacent same-owner plot with the existing plot.
/// Package fees are doubled to cover the merged plot.
/// </summary>
public class PlotMerging : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    public Guid RecordedByUserId { get; set; }

    public string  MergedPlotNumber  { get; set; } = string.Empty;
    public string  MergedPlotSector  { get; set; } = string.Empty;
    public string? MergedPlotSize    { get; set; }
    public string? Notes             { get; set; }
    public string? DocumentUrl       { get; set; }
    public DateTime? ApprovedAt      { get; set; }
}
