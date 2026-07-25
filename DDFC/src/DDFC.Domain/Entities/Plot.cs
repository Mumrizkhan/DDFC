using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class Plot : BaseEntity
{
    public string PlotNumber { get; set; } = string.Empty;
    public string SectorNo { get; set; } = string.Empty;
    public string? StreetNo { get; set; }
    public string PhaseNo { get; set; } = string.Empty;
    public PlotSize PlotSize { get; set; }
    public PlotType PlotType { get; set; }
    public PlotStatus CurrentStatus { get; set; } = PlotStatus.Available;

    // Dimensions for Possession Certificate
    public decimal? LongerSide1 { get; set; }
    public decimal? LongerSide2 { get; set; }
    public decimal? ShorterSide1 { get; set; }
    public decimal? ShorterSide2 { get; set; }

    // Bounded by
    public string? BoundedNorth { get; set; }
    public string? BoundedSouth { get; set; }
    public string? BoundedEast { get; set; }
    public string? BoundedWest { get; set; }

    // Navigation
    public ICollection<PossessionRequest> PossessionRequests { get; set; } = new List<PossessionRequest>();
}
