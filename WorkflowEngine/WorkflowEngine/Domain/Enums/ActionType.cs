namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Defines the type/category of an action within a workflow step
/// </summary>
public enum ActionType
{
    /// <summary>
    /// General/uncategorized action (default)
    /// </summary>
    General = 0,
    
    /// <summary>
    /// Upload action (e.g., document upload, file submission)
    /// </summary>
    Upload = 1,
    
    /// <summary>
    /// Verification action (e.g., verify documents, verify identity)
    /// </summary>
    Verify = 2,
    
    /// <summary>
    /// Check/validation action (e.g., credit check, background check)
    /// </summary>
    Check = 3,
    
    /// <summary>
    /// Review action (e.g., compliance review, code review)
    /// </summary>
    Review = 4,
    
    /// <summary>
    /// Assessment action (e.g., risk assessment, quality assessment)
    /// </summary>
    Assessment = 5,
    
    /// <summary>
    /// Approval action (e.g., manager approval, application approval)
    /// </summary>
    Approval = 6,
    
    /// <summary>
    /// Rejection action (e.g., reject application, decline request)
    /// </summary>
    Rejection = 7,
    
    /// <summary>
    /// Generation action (e.g., generate contract, generate report)
    /// </summary>
    Generate = 8,
    
    /// <summary>
    /// Decision-making action (e.g., make final decision)
    /// </summary>
    Decision = 9,

    /// <summary>
    /// Signature/signing action (e.g., customer signs undertaking, sign agreement)
    /// </summary>
    Signature = 10
}
