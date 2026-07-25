using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class PackageLineItem : BaseEntity
{
    public Guid PackageId { get; set; }
    public Package Package { get; set; } = null!;
    public string ServiceName { get; set; } = string.Empty;
    public decimal AmountDDFC { get; set; }
    public decimal AmountExclusive { get; set; }
    public bool IsFree { get; set; } = false;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
