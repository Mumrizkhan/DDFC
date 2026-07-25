namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Base entity with audit fields for tracking entity lifecycle
/// All entities should inherit from this to get automatic audit tracking
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// Generated as a new GUID when the entity is created
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// UTC timestamp when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User/system that created the entity
    /// Format: "user_id", "system", "admin_user_123", etc.
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// UTC timestamp when the entity was last modified
    /// Null if never modified after creation
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// User/system that last modified the entity
    /// Null if never modified after creation
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Soft delete flag - if true, entity is considered deleted but retained in database
    /// Enables audit trail and potential recovery
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// UTC timestamp when the entity was soft deleted
    /// Null if not deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// User/system that deleted the entity
    /// Null if not deleted
    /// </summary>
    public string? DeletedBy { get; set; }
}
