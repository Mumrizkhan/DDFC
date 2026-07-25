using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

/// <summary>
/// Architecture-stage undertaking issued at steps PA Review / Architecture / 3D / Structure / MEP.
/// Holds the request for a configurable number of days once the customer signs.
/// </summary>
public class ArchitectUndertaking : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    public Guid IssuedByUserId { get; set; }

    public string? SignedDocumentUrl { get; set; }
    public int?    HoldDays         { get; set; }
    public DateTime? HoldStartDate  { get; set; }
    public DateTime? HoldEndDate    { get; set; }
    public DateTime? SignedAt       { get; set; }
    public string?   Notes         { get; set; }
}
