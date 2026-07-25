using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class Template : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;   // PossessionCertificate, PaymentChallan, etc.
    public SurveyLanguage Language { get; set; } = SurveyLanguage.EN;
    public string Content { get; set; } = string.Empty;        // Razor HTML
    public int Version { get; set; } = 1;
    public Guid? UpdatedBy { get; set; }
    public new DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
