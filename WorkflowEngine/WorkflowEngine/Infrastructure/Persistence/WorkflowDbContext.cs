using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    public DbSet<Process> Processes { get; set; } = null!;
    public DbSet<ProcessStep> ProcessSteps { get; set; } = null!;
    public DbSet<StepAction> StepActions { get; set; } = null!;
    public DbSet<StepTransition> StepTransitions { get; set; } = null!;

    public DbSet<Request> Requests { get; set; } = null!;
    public DbSet<RequestStep> RequestSteps { get; set; } = null!;
    public DbSet<RequestAction> RequestActions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for all tables
        modelBuilder.HasDefaultSchema("workflow");

        // Configure table names with schema (explicit configuration)
        modelBuilder.Entity<Process>().ToTable("Processes", "workflow");
        modelBuilder.Entity<ProcessStep>().ToTable("ProcessSteps", "workflow");
        modelBuilder.Entity<StepAction>().ToTable("StepActions", "workflow");
        modelBuilder.Entity<StepTransition>().ToTable("StepTransitions", "workflow");
        modelBuilder.Entity<Request>().ToTable("Requests", "workflow");
        modelBuilder.Entity<RequestStep>().ToTable("RequestSteps", "workflow");
        modelBuilder.Entity<RequestAction>().ToTable("RequestActions", "workflow");

        // Configure Process relationships
        modelBuilder.Entity<Process>()
            .HasMany(p => p.Steps)
            .WithOne(s => s.Process)
            .HasForeignKey(s => s.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessStep>()
            .HasMany(s => s.Actions)
            .WithOne(a => a.ProcessStep)
            .HasForeignKey(a => a.ProcessStepId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StepTransition>()
            .HasOne(t => t.FromStep)
            .WithMany(s => s.TransitionsFrom)
            .HasForeignKey(t => t.FromStepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StepTransition>()
            .HasOne(t => t.ToStep)
            .WithMany(s => s.TransitionsTo)
            .HasForeignKey(t => t.ToStepId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Request relationships
        modelBuilder.Entity<Request>()
            .HasMany(r => r.Steps)
            .WithOne(s => s.Request)
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fix cascade path: Change ProcessStepId to Restrict to avoid multiple cascade paths
        // Process ? ProcessSteps ? RequestSteps (would be CASCADE ? CASCADE)
        // Process ? Requests ? RequestSteps (would be CASCADE ? CASCADE)
        // This creates a cycle, so we break it by using Restrict on ProcessStepId
        modelBuilder.Entity<RequestStep>()
            .HasOne(s => s.ProcessStep)
            .WithMany()
            .HasForeignKey(s => s.ProcessStepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RequestStep>()
            .HasMany(s => s.Actions)
            .WithOne(a => a.RequestStep)
            .HasForeignKey(a => a.RequestStepId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fix cascade path: Change StepActionId to Restrict to avoid multiple cascade paths
        // ProcessSteps ? StepActions ? RequestActions (would be CASCADE ? CASCADE)
        // RequestSteps ? RequestActions (would be CASCADE)
        // This creates multiple paths, so we break it by using Restrict on StepActionId
        modelBuilder.Entity<RequestAction>()
            .HasOne(a => a.StepAction)
            .WithMany()
            .HasForeignKey(a => a.StepActionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure soft delete global query filter
        modelBuilder.Entity<Process>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProcessStep>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StepAction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StepTransition>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Request>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestStep>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestAction>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    // CreatedBy should be set by the application layer
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // UpdatedBy should be set by the application layer
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    // DeletedBy should be set by the application layer
                    break;
            }
        }
    }
}
