using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

/// <summary>
/// Records a customer's signed undertaking accepting a delay in the possession/design workflow.
/// Can be initiated either proactively by the customer or requested by DDFC staff.
/// </summary>
public class DelayUndertaking : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    /// <summary>Who triggered the undertaking — Customer or DDFC.</summary>
    public DelayUndertakingInitiator InitiatedBy { get; set; }

    // ── DDFC-requested path ─────────────────────────────────────────────────
    /// <summary>DDFC staff member who requested the undertaking (null if customer-initiated).</summary>
    public Guid? RequestedByUserId { get; set; }

    /// <summary>When DDFC formally requested the customer to sign.</summary>
    public DateTime? RequestedAt { get; set; }

    // ── Customer signing ────────────────────────────────────────────────────
    /// <summary>Customer who signed the undertaking.</summary>
    public Guid? SignedByCustomerId { get; set; }

    /// <summary>Timestamp when the customer signed.</summary>
    public DateTime? SignedAt { get; set; }

    // ── Delay details ───────────────────────────────────────────────────────
    /// <summary>Reason for the delay (optional, may be filled by DDFC when requesting).</summary>
    public string? DelayReason { get; set; }

    /// <summary>Estimated delay in days.</summary>
    public int? ExpectedDelayDays { get; set; }

    /// <summary>URL of the uploaded signed undertaking document.</summary>
    public string? UndertakingDocumentUrl { get; set; }

    /// <summary>Additional notes attached to the undertaking.</summary>
    public string? Notes { get; set; }
}

public enum DelayUndertakingInitiator
{
    /// <summary>The customer proactively initiated the undertaking.</summary>
    Customer = 0,

    /// <summary>DDFC staff requested the customer to sign the undertaking.</summary>
    DDFC = 1,
}
