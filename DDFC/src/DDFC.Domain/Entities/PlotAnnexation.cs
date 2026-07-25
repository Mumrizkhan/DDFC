using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

/// <summary>
/// Records the annexation of adjacent extra land to the existing plot.
/// An additional fee (configurable, default 15 000 PKR) is charged.
/// </summary>
public class PlotAnnexation : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    public Guid RecordedByUserId { get; set; }

    /// <summary>Human-readable area description, e.g. "2 Marla", "200 sq ft".</summary>
    public string? AdditionalArea { get; set; }

    public decimal AnnexationFee { get; set; } = 15_000m;
    public string? Notes         { get; set; }
    public string? DocumentUrl   { get; set; }
    public DateTime? ApprovedAt  { get; set; }
}
