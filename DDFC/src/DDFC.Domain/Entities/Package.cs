using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class Package : BaseEntity
{
    public PlotType PlotType { get; set; }
    public PlotSize PlotSize { get; set; }
    public PackageTier PackageTier { get; set; }
    public PackageCategory PackageCategory { get; set; } = PackageCategory.HouseDesign;
    public DesignType DesignType { get; set; } = DesignType.InclusiveDesign;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;

    // Navigation
    public ICollection<PackageLineItem> LineItems { get; set; } = new List<PackageLineItem>();
    public ICollection<PossessionRequest> Requests { get; set; } = new List<PossessionRequest>();
}
