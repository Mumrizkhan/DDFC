using DDFC.Domain.Common;
using DDFC.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DDFC.Infrastructure.Persistence;

public class DDFCDbContext : IdentityDbContext<User, Role, Guid>
{
    public DDFCDbContext(DbContextOptions<DDFCDbContext> options) : base(options) { }

    // Core DDFC entities (Users and Roles are provided by IdentityDbContext)
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Plot> Plots { get; set; } = null!;
    public DbSet<PossessionRequest> PossessionRequests { get; set; } = null!;
    public DbSet<RequestWorkflowHistory> RequestWorkflowHistories { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<PackageLineItem> PackageLineItems { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<PaymentChallan> PaymentChallans { get; set; } = null!;
    public DbSet<ArchitecturalPlan> ArchitecturalPlans { get; set; } = null!;
    public DbSet<PlanRevisionRequest> PlanRevisionRequests { get; set; } = null!;
    public DbSet<ThreeDVisualization> ThreeDVisualizations { get; set; } = null!;
    public DbSet<CadFile> CadFiles { get; set; } = null!;
    public DbSet<StructuralReport> StructuralReports { get; set; } = null!;
    public DbSet<MEPReport> MEPReports { get; set; } = null!;
    public DbSet<SurveyObservation> SurveyObservations { get; set; } = null!;
    public DbSet<SoilTestReport> SoilTestReports { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<PossessionCertificate> PossessionCertificates { get; set; } = null!;
    public DbSet<SurveyForm> SurveyForms { get; set; } = null!;
    public DbSet<DelayUndertaking> DelayUndertakings { get; set; } = null!;
    public DbSet<ArchitectUndertaking> ArchitectUndertakings { get; set; } = null!;
    public DbSet<PlotAnnexation> PlotAnnexations { get; set; } = null!;
    public DbSet<PlotMerging> PlotMergings { get; set; } = null!;
    public DbSet<CadAssignment> CadAssignments { get; set; } = null!;

    // Interface support entities
    public DbSet<TaskAssignment> TaskAssignments { get; set; } = null!;
    public DbSet<CustomerNotification> CustomerNotifications { get; set; } = null!;
    public DbSet<SupportTicket> SupportTickets { get; set; } = null!;
    public DbSet<TicketReply> TicketReplies { get; set; } = null!;
    public DbSet<EmployeeAvailability> EmployeeAvailabilities { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // Identity configuration must run first

        modelBuilder.HasDefaultSchema("ddfc");

        // ── Identity table names (under identity schema) ──────────────────────────
        modelBuilder.Entity<User>()                           .ToTable("Users", "identity");
        modelBuilder.Entity<Role>()                           .ToTable("Roles", "identity");
        modelBuilder.Entity<IdentityUserRole<Guid>>()         .ToTable("UserRoles", "identity");
        modelBuilder.Entity<IdentityUserClaim<Guid>>()        .ToTable("UserClaims", "identity");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>()        .ToTable("RoleClaims", "identity");
        modelBuilder.Entity<IdentityUserLogin<Guid>>()        .ToTable("UserLogins", "identity");
        modelBuilder.Entity<IdentityUserToken<Guid>>()        .ToTable("UserTokens", "identity");

        // Soft delete filters
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Plot>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PossessionRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);

        // PossessionRequest indexes
        modelBuilder.Entity<PossessionRequest>()
            .HasIndex(r => r.RequestId).IsUnique();
        modelBuilder.Entity<PossessionRequest>()
            .HasIndex(r => r.Status);

        // Appointment – multiple User FKs need explicit configuration
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Request).WithMany().HasForeignKey(a => a.RequestId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Department).WithMany().HasForeignKey(a => a.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.AssignedEmployee).WithMany().HasForeignKey(a => a.AssignedEmployeeId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.BookedByUser).WithMany().HasForeignKey(a => a.BookedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DepartmentId, a.AppointmentDate, a.StartTime });
        modelBuilder.Entity<PossessionRequest>()
            .HasOne(r => r.SelectedPackage)
            .WithMany(p => p.Requests)
            .HasForeignKey(r => r.SelectedPackageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PossessionRequest>()
            .HasOne(r => r.SelectedInteriorDesignPackage)
            .WithMany()
            .HasForeignKey(r => r.SelectedInteriorDesignPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PossessionRequest>()
            .HasOne(r => r.SelectedSupervisionPackage)
            .WithMany()
            .HasForeignKey(r => r.SelectedSupervisionPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer unique CNIC
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CNIC).IsUnique();

        // Department – head user (no cascade to avoid cycles)
        modelBuilder.Entity<Department>()
            .HasOne(d => d.HeadUser)
            .WithMany()
            .HasForeignKey(d => d.HeadUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // User – department (many users per dept)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // RequestWorkflowHistory – append-only (no cascade from User)
        modelBuilder.Entity<RequestWorkflowHistory>()
            .HasOne(h => h.ActionByUser)
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // TaskAssignment relationships
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(t => t.AssignedToUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // SurveyObservation – department
        modelBuilder.Entity<SurveyObservation>()
            .HasOne(s => s.Department)
            .WithMany()
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // PossessionRequest – Package (nullable)
        modelBuilder.Entity<PossessionRequest>()
            .HasOne(r => r.SelectedPackage)
            .WithMany(p => p.Requests)
            .HasForeignKey(r => r.SelectedPackageId)
            .OnDelete(DeleteBehavior.SetNull);

        // PossessionRequest – AssignedArchitect
        modelBuilder.Entity<PossessionRequest>()
            .HasOne(r => r.AssignedArchitect)
            .WithMany()
            .HasForeignKey(r => r.AssignedArchitectId)
            .OnDelete(DeleteBehavior.SetNull);

        // PossessionCertificate – one-to-one
        modelBuilder.Entity<PossessionCertificate>()
            .HasOne(c => c.Request)
            .WithOne(r => r.PossessionCertificate)
            .HasForeignKey<PossessionCertificate>(c => c.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // SoilTestReport – one-to-one
        modelBuilder.Entity<SoilTestReport>()
            .HasOne(s => s.Request)
            .WithOne(r => r.SoilTestReport)
            .HasForeignKey<SoilTestReport>(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // DelayUndertaking – one-to-one
        modelBuilder.Entity<DelayUndertaking>()
            .HasOne(d => d.Request)
            .WithOne(r => r.DelayUndertaking)
            .HasForeignKey<DelayUndertaking>(d => d.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // ArchitectUndertaking – one-to-one
        modelBuilder.Entity<ArchitectUndertaking>()
            .HasOne(u => u.Request)
            .WithOne(r => r.ArchitectUndertaking)
            .HasForeignKey<ArchitectUndertaking>(u => u.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlotAnnexation – one-to-one
        modelBuilder.Entity<PlotAnnexation>()
            .HasOne(a => a.Request)
            .WithOne(r => r.PlotAnnexation)
            .HasForeignKey<PlotAnnexation>(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlotMerging – one-to-one
        modelBuilder.Entity<PlotMerging>()
            .HasOne(m => m.Request)
            .WithOne(r => r.PlotMerging)
            .HasForeignKey<PlotMerging>(m => m.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // CadAssignment – many-to-one request, no cascade on user
        modelBuilder.Entity<CadAssignment>()
            .HasOne(ca => ca.Request)
            .WithMany(r => r.CadAssignments)
            .HasForeignKey(ca => ca.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CadAssignment>()
            .HasOne(ca => ca.AssignedUser)
            .WithMany()
            .HasForeignKey(ca => ca.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment – one-to-one challan
        modelBuilder.Entity<PaymentChallan>()
            .HasOne(c => c.Payment)
            .WithOne(p => p.Challan)
            .HasForeignKey<PaymentChallan>(c => c.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // SupportTicket – assigned department (no cascade)
        modelBuilder.Entity<SupportTicket>()
            .HasOne(t => t.AssignedDepartment)
            .WithMany(d => d.AssignedTickets)
            .HasForeignKey(t => t.AssignedDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
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
        // Update BaseEntity-derived entities
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        // Update User (extends IdentityUser, not BaseEntity)
        foreach (var entry in ChangeTracker.Entries<User>().Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        // Update Role (extends IdentityRole, not BaseEntity)
        foreach (var entry in ChangeTracker.Entries<Role>().Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;
    }
}
