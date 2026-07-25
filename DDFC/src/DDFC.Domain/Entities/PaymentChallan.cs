using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class PaymentChallan : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public string ChallanNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string BankName { get; set; } = "Bank Alfalah (Islamic)";
    public string IBAN { get; set; } = "PK40ALFH56540050023666769";
    public string AccountTitle { get; set; } = "MicroChip Enterprises (Pvt) Ltd.";
    public string? FileUrl { get; set; }
}
