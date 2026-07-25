using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class SurveyForm : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public SurveyLanguage Language { get; set; } = SurveyLanguage.EN;
    public string? Responses { get; set; }    // JSON
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string CustomerName { get; set; } = string.Empty;
}
