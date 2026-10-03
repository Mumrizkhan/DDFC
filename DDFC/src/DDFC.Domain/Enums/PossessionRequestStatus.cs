namespace DDFC.Domain.Enums;

public enum PossessionRequestStatus
{
    Submitted,
    TransferApproved,
    FinanceApproved,
    PossessionIssued,
    PackageSelected,
    PackagePaid,
    ArchitectAssigned,
    ArchitectureApproved,
    ThreeDCompleted,
    CadCompleted,
    StructureCompleted,
    MEPCompleted,
    PrincipalArchitectApproved,
    TownPlanningCompleted,
    BuildingControlCompleted,
    FinalApproved,
    Delivered,
    Rejected,
    OnHold,
    Initiated,
    BothBranchesCleared,       // Transfer + Finance both approved; waiting for Building Control to issue cert
    AdminReviewPending,        // Payment completed; awaiting Admin approve/reject review
    BcdLetterUploaded,         // BCD uploaded possession letter; awaiting DDFC Admin signature
    ThreeDDraftPending,        // 3D finalized; Architect must assign drafter and receive draft
    ThreeDDraftUploaded,       // Architect uploaded the completed 3D draft; awaiting Structure
    PossessionLetterSigned,    // DDFC Admin signed the possession letter after it was issued
    DelayUndertakingRequested, // DDFC has requested the customer to sign a delay undertaking
    DelayUndertakingSigned,    // Customer has signed the delay undertaking; workflow advances to package selection
    DocumentsVerification,          // Admin verified the attached documents; workflow is already active
    SoilTestCompleted,          // Soil test uploaded; awaiting Principal Architect initial review (architect assignment)
    AdCoordApproved,             // Assistant Director Coordinator approved; awaiting DDFC Admin to sign possession letter
    PrincipalArchitectReviewPending
}
