using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class PossessionCertificate : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TemplateVersion { get; set; } = 1;
    public string? FileUrl { get; set; }
    public DateTime ValidUntil { get; set; }   // configurable, default 6 months from GeneratedAt

    // Handover parties
    public string? HandedOverBy { get; set; }
    public DateTime? HandedOverDate { get; set; }
    public string? TakenOverBy { get; set; }
    public DateTime? TakenOverDate { get; set; }

    // Stamps
    public string? ChiefSurveyorName { get; set; }
    public string? AdTpBcdName { get; set; }          // AD TP & BCD officer name
}
