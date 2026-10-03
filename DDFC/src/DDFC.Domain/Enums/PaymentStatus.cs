namespace DDFC.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled,
    /// <summary>Challan uploaded by Reception; awaiting Possession Admin approval.</summary>
    PendingApproval
}
