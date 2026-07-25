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
    PossessionLetterSigned,    // DDFC Admin signed the possession letter after it was issued
    DelayUndertakingRequested, // DDFC has requested the customer to sign a delay undertaking
    DelayUndertakingSigned,    // Customer has signed the delay undertaking; workflow advances to package selection
}
