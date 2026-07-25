using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;           // 13-digit
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? PasswordHash { get; set; }                  // OTP-based login, no password
    public string? CurrentOtp { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PreferredSmsLanguage { get; set; } = "EN";  // EN or UR

    // Navigation
    public ICollection<PossessionRequest> PossessionRequests { get; set; } = new List<PossessionRequest>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<CustomerNotification> Notifications { get; set; } = new List<CustomerNotification>();
}
