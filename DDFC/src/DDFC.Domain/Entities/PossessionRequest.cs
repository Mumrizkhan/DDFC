using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class PossessionRequest : BaseEntity
{
    // Format: DDFC-YYYY-NNNNN
    public string RequestId { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid PlotId { get; set; }
    public Plot Plot { get; set; } = null!;

    // Form 1 Fields
    public string FileNo { get; set; } = string.Empty;
    public string MembershipDPRNo { get; set; } = string.Empty;
    public string OwnerTitle { get; set; } = "Mr";             // Mr/Mrs/Miss
    public string OwnerName { get; set; } = string.Empty;      // Registered owner full name
    public string GuardianName { get; set; } = string.Empty;   // S/o D/o W/o
    public string GuardianRelation { get; set; } = "S/o";
    public string? Contractor { get; set; }

    // Admin Review Step
    public string? AllotmentLetterUrl { get; set; }
    public string? CnicUrl { get; set; }
    public string? AdminRejectionReason { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
    // Guardian-specific (when GuardianRelation != "Self")
    public string? MessageScreenshotUrl { get; set; }
    public string? EStampPaperUrl { get; set; }
    public string? AuthorizedPersonCnicUrl { get; set; }
    public string? AuthorizedPersonPhone { get; set; }

    public PossessionRequestStatus Status { get; set; } = PossessionRequestStatus.Submitted;
    public RequestType RequestType { get; set; } = RequestType.PossessionDesign;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Parallel branch flags
    public bool TransferApproved { get; set; } = false;
    public bool FinanceApproved { get; set; } = false;
    public decimal? AdcAmount { get; set; }          // Additional Development Charges set by Finance
    public bool StructureCompleted { get; set; } = false;
    public bool MEPCompleted { get; set; } = false;
    public string? SoilTestReportUrl { get; set; }
    public bool TownPlanningCompleted { get; set; } = false;
    public bool BuildingControlCompleted { get; set; } = false;
    public bool CustomerApprovedPlan { get; set; } = false;

    // Principal Architect initial assignment
    public Guid? AssignedArchitectId { get; set; }
    public User? AssignedArchitect { get; set; }

    // Package
    public Guid? SelectedPackageId { get; set; }
    public Package? SelectedPackage { get; set; }
    public Guid? SelectedInteriorDesignPackageId { get; set; }
    public Package? SelectedInteriorDesignPackage { get; set; }
    public Guid? SelectedSupervisionPackageId { get; set; }
    public Package? SelectedSupervisionPackage { get; set; }

    // WorkflowEngine Request ID (links to WE engine)
    public Guid? WorkflowRequestId { get; set; }

    // Navigation
    public ICollection<RequestWorkflowHistory> WorkflowHistory { get; set; } = new List<RequestWorkflowHistory>();
    public ICollection<ArchitecturalPlan> ArchitecturalPlans { get; set; } = new List<ArchitecturalPlan>();
    public ICollection<ThreeDVisualization> ThreeDVisualizations { get; set; } = new List<ThreeDVisualization>();
    public ICollection<CadFile> CadFiles { get; set; } = new List<CadFile>();
    public DelayUndertaking? DelayUndertaking { get; set; }
    public ICollection<StructuralReport> StructuralReports { get; set; } = new List<StructuralReport>();
    public ICollection<MEPReport> MEPReports { get; set; } = new List<MEPReport>();
    public ICollection<SurveyObservation> SurveyObservations { get; set; } = new List<SurveyObservation>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public PossessionCertificate? PossessionCertificate { get; set; }
    public SoilTestReport? SoilTestReport { get; set; }
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public ICollection<CustomerNotification> CustomerNotifications { get; set; } = new List<CustomerNotification>();

    // New feature navigation properties
    public ArchitectUndertaking? ArchitectUndertaking { get; set; }
    public PlotAnnexation? PlotAnnexation { get; set; }
    public PlotMerging? PlotMerging { get; set; }
    public ICollection<CadAssignment> CadAssignments { get; set; } = new List<CadAssignment>();
}
