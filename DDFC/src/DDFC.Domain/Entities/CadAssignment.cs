using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

/// <summary>
/// Tracks a per-department CAD assignment for a possession request.
/// CadType is one of: Architecture | ThreeD | Structure | MEP
/// </summary>
public class CadAssignment : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    /// <summary>Architecture | ThreeD | Structure | MEP</summary>
    public string CadType { get; set; } = string.Empty;

    public Guid AssignedUserId   { get; set; }
    public User AssignedUser     { get; set; } = null!;
    public DateTime AssignedAt   { get; set; } = DateTime.UtcNow;

    public string?   FileUrl      { get; set; }
    public string?   FileName     { get; set; }
    public string?   FileType     { get; set; }
    public DateTime? CompletedAt  { get; set; }
}
