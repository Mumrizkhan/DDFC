using DDFC.Domain.Entities;
using DDFC.Domain.Enums;

namespace DDFC.Application.Interfaces;

public interface IPossessionRequestService
{
    Task<PossessionRequest> CreateRequestAsync(CreateRequestDto dto, Guid? createdByUserId = null);
    Task<PossessionRequest> InitiateRequestAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> GetRequestAsync(Guid requestId);
    Task<PossessionRequest> GetRequestByStringIdAsync(string requestId);
    Task<IEnumerable<PossessionRequest>> GetRequestsByCustomerAsync(Guid customerId);
    Task<PossessionRequest> ApproveTransferAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> RejectTransferAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> RequestTransferClarificationAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> ApproveFinanceAsync(Guid requestId, Guid userId, string? comments, decimal? adcAmount = null);
    Task<PossessionRequest> RejectFinanceAsync(Guid requestId, Guid userId, string? comments);
    /// <summary>Assistant Director Coordinator approves — runs after Finance, before DDFC Admin signs the possession letter.</summary>
    Task<PossessionRequest> ApproveAdCoordAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> RejectAdCoordAsync(Guid requestId, Guid userId, string? comments);
    /// <summary>BCD uploads the possession letter after AD Coordinator approval.</summary>
    Task<PossessionRequest> UploadBcdPossessionLetterAsync(Guid requestId, Guid userId, string possessionLetterFileUrl, string? comments);
    Task<PossessionRequest> DdfcAdminSignAsync(Guid requestId, Guid userId, IssueCertDto dto);
    Task<PossessionCertificate?> GetPossessionCertificateAsync(Guid requestId);
    Task<PossessionRequest> SelectPackageAsync(Guid requestId, Guid packageId, Guid? interiorDesignPackageId, Guid? supervisionPackageId, Guid userId);
    Task<PossessionRequest> ConfirmPaymentAsync(Guid requestId, Guid userId, decimal amountPaid, string? challanNo = null, string? scannedFileUrl = null);
    /// <summary>Reception uploads the paid challan; sends it to Possession Admin for approval.</summary>
    Task<PossessionRequest> SubmitPaymentChallanAsync(Guid requestId, Guid userId, string challanNo, string scannedFileUrl);
    /// <summary>Possession Admin approves the uploaded challan and advances the workflow.</summary>
    Task<PossessionRequest> ApprovePaymentAsync(Guid requestId, Guid userId, decimal? amountPaid = null);
    Task<PossessionRequest> UploadPlanAsync(Guid requestId, string fileUrl, Guid userId, string? notes);
    Task<PossessionRequest> ApprovePlanAsync(Guid requestId, Guid planId, Guid customerId);
    Task<PossessionRequest> RequestPlanRevisionAsync(Guid requestId, Guid planId, Guid customerId, string comments, string? markupUrl);
    Task<PossessionRequest> PAInitialReviewAsync(Guid requestId, Guid assignedArchitectId, SoilTestDto? soilTest, string? notes, Guid userId);
    /// <summary>Completes the standalone "Soil Test" step (before Principal Architect Initial Review). DDFC workflow only.</summary>
    Task<PossessionRequest> SubmitInitialSoilTestAsync(Guid requestId, SoilTestDto dto, Guid userId);
    Task<PossessionRequest> UploadThreeDFileAsync(Guid requestId, ThreeDFileDto dto, Guid userId);
    Task<PossessionRequest> CompleteThreeDAsync(Guid requestId, Guid userId);
    Task<PossessionRequest> AssignThreeDDrafterAsync(Guid requestId, Guid userId, Guid drafterId);
    Task<PossessionRequest> CompleteThreeDDraftAsync(Guid requestId, Guid userId);
    Task<PossessionRequest> UploadCadFileAsync(Guid requestId, CadFileDto dto, Guid userId);
    Task<PossessionRequest> CompleteCadAsync(Guid requestId, Guid userId);
    Task<PossessionRequest> CompleteStructureAsync(Guid requestId, string reportFileUrl, string? observations, Guid userId);
    Task<PossessionRequest> CompleteMEPAsync(Guid requestId, string reportFileUrl, string? observations, Guid userId);
    Task<PossessionRequest> TryCompleteMEPAsync(Guid requestId, Guid userId);
    Task<PossessionRequest> PrincipalApproveAsync(Guid requestId, Guid userId, bool approved, string? comments);
    Task<PossessionRequest> UploadSoilTestAsync(Guid requestId, SoilTestDto dto, Guid userId);
    Task<PossessionRequest> SubmitBuildingControlAsync(Guid requestId, BuildingControlDto dto, Guid userId);
    Task<PossessionRequest> CompleteBuildingControlAsync(Guid requestId, Guid userId);
    Task<PossessionRequest> FinalApproveAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> RejectRequestAsync(Guid requestId, Guid userId, string? comments);
    Task<PossessionRequest> VerifyDocumentsAsync(Guid requestId, Guid userId, string action, string? comments);
    Task<PossessionRequest> AdminReviewAsync(Guid requestId, AdminReviewDto dto, Guid userId);
    Task<PossessionRequest> DeliverDocumentsAsync(Guid requestId, Guid userId, SurveyLanguage surveyLanguage);
    Task<IEnumerable<RequestWorkflowHistory>> GetWorkflowHistoryAsync(Guid requestId);
    Task LogHistoryAsync(Guid requestId, string? fromStatus, string toStatus, string? action, Guid? userId, string? comments);
    Task<IEnumerable<string>> GetActiveWorkflowStepNamesAsync(Guid requestId);

    // ── Delay Undertaking ────────────────────────────────────────────────────
    /// <summary>
    /// DDFC staff request the customer to sign a delay undertaking.
    /// Moves the request to DelayUndertakingRequested status.
    /// </summary>
    Task<PossessionRequest> RequestDelayUndertakingAsync(Guid requestId, Guid userId, string? delayReason, int? expectedDelayDays, string? notes);

    /// <summary>
    /// The customer signs the delay undertaking.
    /// Can finalize either a DDFC-requested undertaking or a customer-initiated one.
    /// Advances the workflow to the Package Selection step.
    /// </summary>
    Task<PossessionRequest> SignDelayUndertakingAsync(Guid requestId, Guid customerId, SignDelayUndertakingDto dto);

    /// <summary>
    /// DDFC staff skip the delay undertaking step entirely.
    /// No undertaking is recorded; the workflow advances directly to Package Selection.
    /// </summary>
    Task<PossessionRequest> SkipDelayUndertakingAsync(Guid requestId, Guid userId);

    // ── Document attachment ────────────────────────────────────────────────────
    /// <summary>
    /// Attach a typed document (e.g. CNIC, NOC/NDC Form) to a request.
    /// At most two documents per DocumentType are allowed.
    /// </summary>
    Task<Document> AttachDocumentAsync(Guid requestId, DocumentType documentType, string fileUrl, Guid uploadedBy);
}

public record AdminReviewDto(
    string Action, // "Approve" or "Reject"
    string? RejectionReason);

public record DocumentVerificationDto(
    string Action, // "Approve" or "Incomplete"
    string? Comments);

public record CreateRequestDto(
    Guid CustomerId, Guid PlotId, string PlotNumber, string SectorNo, string PhaseNo,
    string FileNo, string MembershipDPRNo,
    string OwnerTitle, string OwnerName, string SonDaughterWifeOf, string GuardianRelation,
    string? AuthorizedPersonName = null,
    string? Contractor = null,
    RequestType RequestType = RequestType.PossessionDesign,
    Guid? LinkedPossessionRequestId = null);

public record SoilTestDto(
    DateTime TestDate, string LabName, string SoilBearingCapacity,
    string ResultSummary, string ReportFileUrl);

public record BuildingControlDto(
    DateTime SurveyDate, string OfficerName, string? Observations,
    string? Violations, string? Suggestions, string? PhotoUrlsJson);

public record IssueCertDto(
    string? HandedOverBy,
    DateTime? HandedOverDate,
    string? TakenOverBy,
    DateTime? TakenOverDate,
    string? ChiefSurveyorName,
    string? AdTpBcdName,
    string? PossessionLetterFileUrl = null);

public record ThreeDFileDto(
    string FileUrl,
    string FileName,
    string FileType,
    string? Notes);

public record CadFileDto(
    string FileUrl,
    string FileName,
    string FileType,
    string? Notes);

public record SignDelayUndertakingDto(
    /// <summary>URL of the uploaded signed undertaking document (optional).</summary>
    string? UndertakingDocumentUrl,
    /// <summary>Additional notes from the customer about the delay acceptance.</summary>
    string? Notes);
