using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class Laboratory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}