using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DDFC.Infrastructure.Data;

public static class DDFCDataSeeder
{
    public static async Task SeedAsync(
        DDFCDbContext        db,
        UserManager<User>    userManager,
        RoleManager<Role>    roleManager)
    {
        // ══════════════════════════════════════════════════════════════════════
        // ── STEP 5/6: Plots & Customers — always run (own guards inside) ──────
        // ══════════════════════════════════════════════════════════════════════
        await SeedPlotsAsync(db);
        await SeedCustomersAsync(db);

        // ── Seed new workflow packages independently (run even after initial seed) ──
        if (!db.Packages.Any(p => p.PackageCategory == PackageCategory.RevisedPlan))
        {
            SeedRevisedPlanPackages(db);
            await db.SaveChangesAsync();
        }
        if (!db.Packages.Any(p => p.PackageCategory == PackageCategory.AsBuiltPlan))
        {
            SeedAsBuiltPlanPackages(db);
            await db.SaveChangesAsync();
        }

        // Guard – skip roles/users/packages if already seeded
        if (await roleManager.Roles.AnyAsync()) return;

        // ══════════════════════════════════════════════════════════════════════
        // ── STEP 1: Seed Departments ──────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════
        var frontDesk   = new Department { DepartmentName = "Front Desk",         DepartmentCode = "FD" };
        var transfer    = new Department { DepartmentName = "Transfer Branch",     DepartmentCode = "TB" };
        var finance     = new Department { DepartmentName = "Finance Branch",      DepartmentCode = "FB" };
        var buildCtrl   = new Department { DepartmentName = "Building Control",    DepartmentCode = "BC" };
        var archDept    = new Department { DepartmentName = "Architecture Dept",   DepartmentCode = "AD" };
        var structDept  = new Department { DepartmentName = "Structure Dept",      DepartmentCode = "SD" };
        var mepDept     = new Department { DepartmentName = "MEP Dept",            DepartmentCode = "MD" };
        var principalOfc= new Department { DepartmentName = "Principal Office",    DepartmentCode = "PO" };
        var dhaDesign   = new Department { DepartmentName = "DHA Design Dept",     DepartmentCode = "DD" };
        var adminDept   = new Department { DepartmentName = "Administration",      DepartmentCode = "AS" };
        var techSupDept = new Department { DepartmentName = "Technical Support",   DepartmentCode = "TS" };

        db.Departments.AddRange(frontDesk, transfer, finance, buildCtrl,
            archDept, structDept, mepDept, principalOfc, dhaDesign, adminDept, techSupDept);
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════════════
        // ── STEP 2: Seed Roles with Role Claims ───────────────────────────────
        // ══════════════════════════════════════════════════════════════════════
        
        // Admin Role
        var adminRole = new Role("Admin") { IsBuiltIn = true };
        await roleManager.CreateAsync(adminRole);
        await AddRoleClaims(roleManager, adminRole, 
            "CanManageUsers", "CanManageCustomers", "CanConfigurePackages", "CanViewAllRequests", 
            "CanViewReports", "CanManageTemplates", "CanManageDepartments", 
            "CanManageRoles", "CanManageSettings", "CanResetRoundRobin");

        // Reception Officer Role
        var receptionRole = new Role("Reception Officer") { IsBuiltIn = true };
        await roleManager.CreateAsync(receptionRole);
        await AddRoleClaims(roleManager, receptionRole,
            "CanCreateRequest", "CanSelectPackage", "CanConfirmPayment", "CanDeliverDocuments", "CanViewAllRequests");

        // Transfer Officer Role
        var transferRole = new Role("Transfer Officer") { IsBuiltIn = true };
        await roleManager.CreateAsync(transferRole);
        await AddRoleClaims(roleManager, transferRole,
            "CanApproveTransfer", "CanViewAllRequests");

        // Finance Officer Role
        var financeRole = new Role("Finance Officer") { IsBuiltIn = true };
        await roleManager.CreateAsync(financeRole);
        await AddRoleClaims(roleManager, financeRole,
            "CanApproveFinance", "CanConfirmPayment", "CanViewAllRequests");

        // Building Control Officer Role (also handles soil tests and possession cert issuance)
        var buildingControlRole = new Role("Building Control Officer") { IsBuiltIn = true };
        await roleManager.CreateAsync(buildingControlRole);
        await AddRoleClaims(roleManager, buildingControlRole,
            "CanSubmitBuildingControl", "CanUploadSoilTest", "CanIssuePossessionCert", "CanViewAllRequests");

        // Architect Role
        var architectRole = new Role("Architect") { IsBuiltIn = true };
        await roleManager.CreateAsync(architectRole);
        await AddRoleClaims(roleManager, architectRole,
            "CanUploadPlan", "CanViewAllRequests");

        // Structure Engineer Role
        var structureRole = new Role("Structure Engineer") { IsBuiltIn = true };
        await roleManager.CreateAsync(structureRole);
        await AddRoleClaims(roleManager, structureRole,
            "CanCompleteStructure", "CanViewAllRequests");

        // MEP Engineer Role
        var mepRole = new Role("MEP Engineer") { IsBuiltIn = true };
        await roleManager.CreateAsync(mepRole);
        await AddRoleClaims(roleManager, mepRole,
            "CanCompleteMEP", "CanViewAllRequests");

        // Principal Architect Role
        var principalArchitectRole = new Role("Principal Architect") { IsBuiltIn = true };
        await roleManager.CreateAsync(principalArchitectRole);
        await AddRoleClaims(roleManager, principalArchitectRole,
            "CanPrincipalApprove", "CanViewAllRequests");

        // DHA Design Head Role
        var designHeadRole = new Role("DHA Design Head") { IsBuiltIn = true };
        await roleManager.CreateAsync(designHeadRole);
        await AddRoleClaims(roleManager, designHeadRole,
            "CanFinalApprove", "CanViewAllRequests");

        // Technical Support Role
        var techSupportRole = new Role("Technical Support") { IsBuiltIn = true };
        await roleManager.CreateAsync(techSupportRole);
        await AddRoleClaims(roleManager, techSupportRole,
            "CanManageTickets", "CanReplyTickets", "CanResolveTickets", "CanReopenTickets");

        // Possession Admin Role
        var possessionAdminRole = new Role("Possession Admin") { IsBuiltIn = true };
        await roleManager.CreateAsync(possessionAdminRole);
        await AddRoleClaims(roleManager, possessionAdminRole,
            "CanAdminReview", "CanCreateRequest", "CanViewAllRequests");

        // DDFC Admin Role (signs possession letters, selects packages, confirms payments)
        var ddfcAdminRole = new Role("DDFC Admin") { IsBuiltIn = true };
        await roleManager.CreateAsync(ddfcAdminRole);
        await AddRoleClaims(roleManager, ddfcAdminRole,
            "CanSignPossessionLetter", "CanSelectPackage", "CanConfirmPayment", "CanViewAllRequests");

        // ══════════════════════════════════════════════════════════════════════
        // ── STEP 3: Seed Users with User Claims ───────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        // Admin User
        await CreateUserWithClaims(userManager, 
            fullName: "System Administrator",
            email: "admin@ddfc.com.pk",
            password: "Admin@2026!",
            departmentId: adminDept.Id,
            roleName: "Admin",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", adminDept.DepartmentCode),
                ("accessLevel", "SuperAdmin")
            });

        // Reception Officers
        await CreateUserWithClaims(userManager,
            fullName: "Ali Ahmed",
            email: "ali.ahmed@ddfc.com.pk",
            password: "Reception@2026!",
            departmentId: frontDesk.Id,
            roleName: "Reception Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", frontDesk.DepartmentCode),
                ("canInitiateRequests", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Fatima Khan",
            email: "fatima.khan@ddfc.com.pk",
            password: "Reception@2026!",
            departmentId: frontDesk.Id,
            roleName: "Reception Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", frontDesk.DepartmentCode),
                ("canInitiateRequests", "true")
            });

        // Transfer Officers
        await CreateUserWithClaims(userManager,
            fullName: "Hassan Raza",
            email: "hassan.raza@ddfc.com.pk",
            password: "Transfer@2026!",
            departmentId: transfer.Id,
            roleName: "Transfer Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", transfer.DepartmentCode),
                ("approvalAuthority", "Transfer")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Ayesha Malik",
            email: "ayesha.malik@ddfc.com.pk",
            password: "Transfer@2026!",
            departmentId: transfer.Id,
            roleName: "Transfer Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", transfer.DepartmentCode),
                ("approvalAuthority", "Transfer")
            });

        // Finance Officers
        await CreateUserWithClaims(userManager,
            fullName: "Imran Siddiqui",
            email: "imran.siddiqui@ddfc.com.pk",
            password: "Finance@2026!",
            departmentId: finance.Id,
            roleName: "Finance Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", finance.DepartmentCode),
                ("approvalAuthority", "Finance"),
                ("canVerifyPayments", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Sara Baig",
            email: "sara.baig@ddfc.com.pk",
            password: "Finance@2026!",
            departmentId: finance.Id,
            roleName: "Finance Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", finance.DepartmentCode),
                ("approvalAuthority", "Finance"),
                ("canVerifyPayments", "true")
            });

        // Building Control Officers (now also handle possession cert issuance)
        await CreateUserWithClaims(userManager,
            fullName: "Kamran Ali",
            email: "kamran.ali@ddfc.com.pk",
            password: "BuildControl@2026!",
            departmentId: buildCtrl.Id,
            roleName: "Building Control Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", buildCtrl.DepartmentCode),
                ("inspectionAuthority", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Zainab Hussain",
            email: "zainab.hussain@ddfc.com.pk",
            password: "BuildControl@2026!",
            departmentId: buildCtrl.Id,
            roleName: "Building Control Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", buildCtrl.DepartmentCode),
                ("inspectionAuthority", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Bilal Ahmed",
            email: "bilal.ahmed@ddfc.com.pk",
            password: "BuildControl@2026!",
            departmentId: buildCtrl.Id,
            roleName: "Building Control Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", buildCtrl.DepartmentCode),
                ("inspectionAuthority", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Maryam Iqbal",
            email: "maryam.iqbal@ddfc.com.pk",
            password: "BuildControl@2026!",
            departmentId: buildCtrl.Id,
            roleName: "Building Control Officer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", buildCtrl.DepartmentCode),
                ("inspectionAuthority", "true")
            });

        // Architects
        await CreateUserWithClaims(userManager,
            fullName: "Usman Tariq",
            email: "usman.tariq@ddfc.com.pk",
            password: "Architect@2026!",
            departmentId: archDept.Id,
            roleName: "Architect",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", archDept.DepartmentCode),
                ("designSpecialty", "Residential"),
                ("yearsOfExperience", "5")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Sadia Farooq",
            email: "sadia.farooq@ddfc.com.pk",
            password: "Architect@2026!",
            departmentId: archDept.Id,
            roleName: "Architect",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", archDept.DepartmentCode),
                ("designSpecialty", "Commercial"),
                ("yearsOfExperience", "7")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Adnan Sheikh",
            email: "adnan.sheikh@ddfc.com.pk",
            password: "Architect@2026!",
            departmentId: archDept.Id,
            roleName: "Architect",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", archDept.DepartmentCode),
                ("designSpecialty", "Mixed"),
                ("yearsOfExperience", "8")
            });

        // Structure Engineers
        await CreateUserWithClaims(userManager,
            fullName: "Fahad Mirza",
            email: "fahad.mirza@ddfc.com.pk",
            password: "Structure@2026!",
            departmentId: structDept.Id,
            roleName: "Structure Engineer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", structDept.DepartmentCode),
                ("engineeringLicense", "PEC-12345"),
                ("specialization", "SeismicDesign")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Hina Javed",
            email: "hina.javed@ddfc.com.pk",
            password: "Structure@2026!",
            departmentId: structDept.Id,
            roleName: "Structure Engineer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", structDept.DepartmentCode),
                ("engineeringLicense", "PEC-12346"),
                ("specialization", "Foundation")
            });

        // MEP Engineers
        await CreateUserWithClaims(userManager,
            fullName: "Tariq Mahmood",
            email: "tariq.mahmood@ddfc.com.pk",
            password: "MEP@2026!",
            departmentId: mepDept.Id,
            roleName: "MEP Engineer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", mepDept.DepartmentCode),
                ("engineeringLicense", "PEC-12347"),
                ("mepSpecialty", "HVAC")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Nadia Akram",
            email: "nadia.akram@ddfc.com.pk",
            password: "MEP@2026!",
            departmentId: mepDept.Id,
            roleName: "MEP Engineer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", mepDept.DepartmentCode),
                ("engineeringLicense", "PEC-12348"),
                ("mepSpecialty", "Electrical")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Asif Raza",
            email: "asif.raza@ddfc.com.pk",
            password: "MEP@2026!",
            departmentId: mepDept.Id,
            roleName: "MEP Engineer",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", mepDept.DepartmentCode),
                ("engineeringLicense", "PEC-12349"),
                ("mepSpecialty", "Plumbing")
            });

        // Principal Architect
        await CreateUserWithClaims(userManager,
            fullName: "Jawad Abbas",
            email: "jawad.abbas@ddfc.com.pk",
            password: "Principal@2026!",
            departmentId: principalOfc.Id,
            roleName: "Principal Architect",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", principalOfc.DepartmentCode),
                ("seniorApprovalAuthority", "true"),
                ("yearsOfExperience", "15")
            });

        // DHA Design Head
        await CreateUserWithClaims(userManager,
            fullName: "Brigadier (R) Zulfiqar Ali",
            email: "zulfiqar.ali@ddfc.com.pk",
            password: "DesignHead@2026!",
            departmentId: dhaDesign.Id,
            roleName: "DHA Design Head",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", dhaDesign.DepartmentCode),
                ("finalApprovalAuthority", "true"),
                ("rank", "Brigadier")
            });

        // Technical Support Staff
        await CreateUserWithClaims(userManager,
            fullName: "Tech Support Agent",
            email: "support@ddfc.com.pk",
            password: "Support@2026!",
            departmentId: techSupDept.Id,
            roleName: "Technical Support",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", techSupDept.DepartmentCode),
                ("canManageTickets", "true")
            });

        await CreateUserWithClaims(userManager,
            fullName: "Omer Siddique",
            email: "omer.siddique@ddfc.com.pk",
            password: "Support@2026!",
            departmentId: techSupDept.Id,
            roleName: "Technical Support",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", techSupDept.DepartmentCode),
                ("canManageTickets", "true")
            });

        // Possession Admin
        await CreateUserWithClaims(userManager,
            fullName: "Possession Admin",
            email: "possession.admin@ddfc.com.pk",
            password: "PossessionAdmin@2026!",
            departmentId: adminDept.Id,
            roleName: "Possession Admin",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", adminDept.DepartmentCode),
                ("canAdminReview", "true")
            });

        // DDFC Admin (signs possession letters)
        await CreateUserWithClaims(userManager,
            fullName: "DDFC Admin",
            email: "ddfc.admin@ddfc.com.pk",
            password: "DdfcAdmin@2026!",
            departmentId: adminDept.Id,
            roleName: "DDFC Admin",
            claims: new[]
            {
                ("userType", "staff"),
                ("departmentCode", adminDept.DepartmentCode),
                ("canSignPossessionLetter", "true")
            });

        // ══════════════════════════════════════════════════════════════════════
        // ── STEP 4: Seed Packages (from DHA DDFC official pamphlet) ─────────────
        // Two design tracks per size/tier: DdfcInclusive and ExclusiveDesign
        // ══════════════════════════════════════════════════════════════════════
        SeedHouseDesignPackages(db);
        SeedInteriorDesignPackages(db);
        SeedSupervisionPackages(db);

        await db.SaveChangesAsync();
    }

    // ── Revised Plan packages (flat-fee per plot size) ────────────────────────
    private static void SeedRevisedPlanPackages(DDFCDbContext db)
    {
        void Add(PlotSize size, DesignType type, decimal fee)
        {
            var pkg = new Package
            {
                PlotType        = PlotType.Residential,
                PlotSize        = size,
                PackageTier     = PackageTier.Bronze,
                PackageCategory = PackageCategory.RevisedPlan,
                DesignType      = type,
                IsActive        = true,
            };
            pkg.LineItems.Add(Li("Service Fee", fee, false, 1));
            db.Packages.Add(pkg);
        }

        Add(PlotSize.FiveMarla,  DesignType.DdfcInclusive,  20_000m);
        Add(PlotSize.FiveMarla,  DesignType.ExclusiveDesign, 20_000m);
        Add(PlotSize.TenMarla,   DesignType.DdfcInclusive,  30_000m);
        Add(PlotSize.TenMarla,   DesignType.ExclusiveDesign, 30_000m);
        Add(PlotSize.OneKanal,   DesignType.DdfcInclusive,  35_000m);
        Add(PlotSize.OneKanal,   DesignType.ExclusiveDesign, 35_000m);
    }

    // ── As-Built Plan packages (flat-fee per plot size) ───────────────────────
    private static void SeedAsBuiltPlanPackages(DDFCDbContext db)
    {
        void Add(PlotSize size, DesignType type, decimal fee)
        {
            var pkg = new Package
            {
                PlotType        = PlotType.Residential,
                PlotSize        = size,
                PackageTier     = PackageTier.Bronze,
                PackageCategory = PackageCategory.AsBuiltPlan,
                DesignType      = type,
                IsActive        = true,
            };
            pkg.LineItems.Add(Li("Service Fee", fee, false, 1));
            db.Packages.Add(pkg);
        }

        Add(PlotSize.FiveMarla,  DesignType.DdfcInclusive,  15_000m);
        Add(PlotSize.FiveMarla,  DesignType.ExclusiveDesign, 15_000m);
        Add(PlotSize.TenMarla,   DesignType.DdfcInclusive,  25_000m);
        Add(PlotSize.TenMarla,   DesignType.ExclusiveDesign, 25_000m);
        Add(PlotSize.OneKanal,   DesignType.DdfcInclusive,  30_000m);
        Add(PlotSize.OneKanal,   DesignType.ExclusiveDesign, 30_000m);
    }

    // ── House Design packages (exact pricing from DDFC pamphlet) ─────────────

    private static void SeedHouseDesignPackages(DDFCDbContext db)
    {
        // ── RESIDENTIAL: 5 Marla ─────────────────────────────────────────────
        // Residential services (13 rows):
        // Processing Fee | Possession Letter | Site Visit | Soil Test |
        // Architectural Design & Drawing | Structural Design & Drawing |
        // MEP Design & Drawing | 3D Elevation Design & Renders |
        // Interior Design | Material Selection by Professional |
        // Walkthrough Animation | Scrutiny/Vetting Fee | Construction NOC Approval Fee

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 20_000, soilTest: 30_000, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 15_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 20_000, soilTest: 30_000, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 120_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 20_000, soilTest: 30_000, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 15_000, interior: 105_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 20_000, soilTest: 30_000, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 120_000, interior: 45_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 20_000, soilTest: 0 /*Free*/, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 15_000, interior: 105_000, material: 0 /*Free*/, walkthrough: 75_000);

        AddResidentialPackage(db, PlotSize.FiveMarla, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 20_000, soilTest: 0, arch: 80_000, structural: 40_000, mep: 25_000,
            elevation3D: 120_000, interior: 105_000, material: 0, walkthrough: 75_000);

        // ── RESIDENTIAL: 10 Marla ────────────────────────────────────────────
        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 30_000, soilTest: 40_000, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 20_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 30_000, soilTest: 40_000, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 170_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 30_000, soilTest: 40_000, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 20_000, interior: 120_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 30_000, soilTest: 40_000, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 170_000, interior: 46_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 30_000, soilTest: 0, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 20_000, interior: 120_000, material: 0, walkthrough: 100_000);

        AddResidentialPackage(db, PlotSize.TenMarla, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 30_000, soilTest: 0, arch: 125_000, structural: 60_000, mep: 30_000,
            elevation3D: 170_000, interior: 120_000, material: 0, walkthrough: 100_000);

        // ── RESIDENTIAL: 1 Kanal ─────────────────────────────────────────────
        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 50_000, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 50_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 50_000, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 300_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 50_000, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 50_000, interior: 165_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 50_000, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 300_000, interior: 65_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 0, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 50_000, interior: 165_000, material: 0, walkthrough: 150_000);

        AddResidentialPackage(db, PlotSize.OneKanal, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 0, arch: 200_000, structural: 100_000, mep: 50_000,
            elevation3D: 300_000, interior: 165_000, material: 0, walkthrough: 150_000);

        // ── RESIDENTIAL: 2 Kanal (40 Marla) ─────────────────────────────────
        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 80_000, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 80_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 80_000, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 480_000, interior: 0, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 80_000, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 80_000, interior: 250_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 80_000, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 480_000, interior: 100_000, material: 0, walkthrough: 0);

        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 0, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 80_000, interior: 250_000, material: 0, walkthrough: 250_000);

        AddResidentialPackage(db, PlotSize.TwoKanal, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 0, arch: 350_000, structural: 160_000, mep: 80_000,
            elevation3D: 480_000, interior: 250_000, material: 0, walkthrough: 250_000);

        // ── COMMERCIAL: 4 Marla ──────────────────────────────────────────────
        // Commercial services (12 rows — no Material Selection):
        // Processing Fee | Possession Letter | Site Visit | Soil Test |
        // Architectural Design & Drawing | Structural Design & Drawing |
        // MEP Design & Drawing | 3D Elevation Design & Renders |
        // Interior Design | Walkthrough Animation |
        // Scrutiny/Vetting Fee | Construction NOC Approval Fee

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 50_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 350_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 50_000, interior: 250_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 350_000, interior: 100_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 50_000, interior: 250_000, walkthrough: 200_000, landscape: 100_000);

        AddCommercialPackage(db, PlotSize.FourMarla, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 50_000, soilTest: 50_000, arch: 300_000, structural: 100_000, mep: 100_000,
            elevation3D: 350_000, interior: 250_000, walkthrough: 200_000, landscape: 100_000);

        // ── COMMERCIAL: 8 Marla ──────────────────────────────────────────────
        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 35_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 365_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 35_000, interior: 250_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 365_000, interior: 100_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 35_000, interior: 250_000, walkthrough: 250_000, landscape: 125_000);

        AddCommercialPackage(db, PlotSize.EightMarla, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 80_000, soilTest: 100_000, arch: 460_000, structural: 200_000, mep: 180_000,
            elevation3D: 365_000, interior: 250_000, walkthrough: 250_000, landscape: 125_000);

        // ── COMMERCIAL: 1 Kanal ──────────────────────────────────────────────
        // (13 rows — includes Landscape Design)
        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Bronze, DesignType.DdfcInclusive,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 40_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Bronze, DesignType.ExclusiveDesign,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 625_000, interior: 0, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Silver, DesignType.DdfcInclusive,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 40_000, interior: 250_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Silver, DesignType.ExclusiveDesign,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 625_000, interior: 250_000, walkthrough: 0, landscape: 0);

        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Gold, DesignType.DdfcInclusive,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 40_000, interior: 250_000, walkthrough: 400_000, landscape: 400_000);

        AddCommercialPackage(db, PlotSize.OneKanal, PackageTier.Gold, DesignType.ExclusiveDesign,
            processing: 150_000, soilTest: 150_000, arch: 850_000, structural: 350_000, mep: 300_000,
            elevation3D: 625_000, interior: 250_000, walkthrough: 400_000, landscape: 400_000);
    }

    // ── Residential helper ────────────────────────────────────────────────────
    private static void AddResidentialPackage(
        DDFCDbContext db,
        PlotSize size, PackageTier tier, DesignType designType,
        decimal processing, decimal soilTest, decimal arch, decimal structural, decimal mep,
        decimal elevation3D, decimal interior, decimal material, decimal walkthrough)
    {
        var pkg = new Package
        {
            PlotType        = PlotType.Residential,
            PlotSize        = size,
            PackageTier     = tier,
            PackageCategory = PackageCategory.HouseDesign,
            DesignType      = designType,
            IsActive        = true,
        };

        var isSoilFree      = soilTest == 0 && tier == PackageTier.Gold;
        var isMaterialFree  = tier == PackageTier.Gold;

        int n = 1;
        pkg.LineItems.Add(Li("Processing Fee",                   processing,  false, n++));
        pkg.LineItems.Add(Li("Possession Letter",                0m,          true,  n++));
        pkg.LineItems.Add(Li("Site Visit",                       0m,          true,  n++));
        pkg.LineItems.Add(soilTest > 0
            ? Li("Soil Test",                                    soilTest,    false, n++)
            : Li("Soil Test",                                    0m,          true,  n++));
        pkg.LineItems.Add(Li("Architectural Design & Drawing",   arch,        false, n++));
        pkg.LineItems.Add(Li("Structural Design & Drawing",      structural,  false, n++));
        pkg.LineItems.Add(Li("MEP Design & Drawing",             mep,         false, n++));
        pkg.LineItems.Add(Li("3D Elevation Design & Renders",    elevation3D, false, n++));
        if (interior > 0)
            pkg.LineItems.Add(Li("Interior Design",              interior,    false, n++));
        if (tier == PackageTier.Gold)
        {
            pkg.LineItems.Add(Li("Material Selection by Professional", 0m,    true,  n++));
            if (walkthrough > 0)
                pkg.LineItems.Add(Li("Walkthrough Animation",    walkthrough, false, n++));
        }
        pkg.LineItems.Add(Li("Scrutiny/Vetting Fee",             0m,          true,  n++));
        pkg.LineItems.Add(Li("Construction NOC Approval Fee",    0m,          true,  n++));

        db.Packages.Add(pkg);
    }

    // ── Commercial helper ─────────────────────────────────────────────────────
    private static void AddCommercialPackage(
        DDFCDbContext db,
        PlotSize size, PackageTier tier, DesignType designType,
        decimal processing, decimal soilTest, decimal arch, decimal structural, decimal mep,
        decimal elevation3D, decimal interior, decimal walkthrough, decimal landscape)
    {
        var pkg = new Package
        {
            PlotType        = PlotType.Commercial,
            PlotSize        = size,
            PackageTier     = tier,
            PackageCategory = PackageCategory.HouseDesign,
            DesignType      = designType,
            IsActive        = true,
        };

        int n = 1;
        pkg.LineItems.Add(Li("Processing Fee",                   processing,  false, n++));
        pkg.LineItems.Add(Li("Possession Letter",                0m,          true,  n++));
        pkg.LineItems.Add(Li("Site Visit",                       0m,          true,  n++));
        pkg.LineItems.Add(Li("Soil Test",                        soilTest,    false, n++));
        pkg.LineItems.Add(Li("Architectural Design & Drawing",   arch,        false, n++));
        pkg.LineItems.Add(Li("Structural Design & Drawing",      structural,  false, n++));
        pkg.LineItems.Add(Li("MEP Design & Drawing",             mep,         false, n++));
        pkg.LineItems.Add(Li("3D Elevation Design & Renders",    elevation3D, false, n++));
        if (interior > 0)
            pkg.LineItems.Add(Li("Interior Design",              interior,    false, n++));
        if (walkthrough > 0)
            pkg.LineItems.Add(Li("Walkthrough Animation",        walkthrough, false, n++));
        if (landscape > 0)
            pkg.LineItems.Add(Li("Landscape Design",             landscape,   false, n++));
        pkg.LineItems.Add(Li("Scrutiny/Vetting Fee",             0m,          true,  n++));
        pkg.LineItems.Add(Li("Construction NOC Approval Fee",    0m,          true,  n++));

        db.Packages.Add(pkg);
    }

    // ── Interior Design add-on packages ──────────────────────────────────────
    private static void SeedInteriorDesignPackages(DDFCDbContext db)
    {
        foreach (PlotType pType in Enum.GetValues<PlotType>())
        foreach (PlotSize pSize in Enum.GetValues<PlotSize>())
        foreach (PackageTier tier in Enum.GetValues<PackageTier>())
        {
            if (pType == PlotType.Residential && pSize == PlotSize.FourMarla) continue;
            if (pType == PlotType.Commercial && pSize == PlotSize.TwoKanal) continue;

            var baseAmt = pSize switch
            {
                PlotSize.FourMarla  => 40_000m,
                PlotSize.FiveMarla  => 50_000m,
                PlotSize.EightMarla => 75_000m,
                PlotSize.TenMarla   => 100_000m,
                PlotSize.OneKanal   => 175_000m,
                PlotSize.TwoKanal   => 300_000m,
                _                   => 50_000m
            };
            var mult = tier switch { PackageTier.Bronze => 1.0m, PackageTier.Silver => 1.5m, _ => 2.2m };
            var pkg = new Package { PlotType = pType, PlotSize = pSize, PackageTier = tier,
                PackageCategory = PackageCategory.InteriorDesign, DesignType = DesignType.DdfcInclusive, IsActive = true };
            int n = 1;
            pkg.LineItems.Add(Li("Interior Design Consultation",       baseAmt * mult * 0.10m, false, n++));
            pkg.LineItems.Add(Li("Space Planning & Layout",            baseAmt * mult * 0.15m, false, n++));
            pkg.LineItems.Add(Li("3D Interior Visualization",          baseAmt * mult * 0.20m, false, n++));
            if (tier >= PackageTier.Silver)
            {
                pkg.LineItems.Add(Li("Material Selection & Specification", baseAmt * mult * 0.15m, false, n++));
                pkg.LineItems.Add(Li("Furniture Selection & Layout",       baseAmt * mult * 0.15m, false, n++));
            }
            if (tier == PackageTier.Gold)
            {
                pkg.LineItems.Add(Li("Lighting Design",               baseAmt * mult * 0.15m, false, n++));
                pkg.LineItems.Add(Li("Color Scheme & Finish Selection", baseAmt * mult * 0.10m, false, n++));
                pkg.LineItems.Add(Li("Walkthrough Renders",            baseAmt * mult * 0.15m, false, n++));
            }
            db.Packages.Add(pkg);
        }
    }

    // ── Supervision add-on packages ───────────────────────────────────────────
    private static void SeedSupervisionPackages(DDFCDbContext db)
    {
        foreach (PlotType pType in Enum.GetValues<PlotType>())
        foreach (PlotSize pSize in Enum.GetValues<PlotSize>())
        foreach (PackageTier tier in Enum.GetValues<PackageTier>())
        {
            if (pType == PlotType.Residential && pSize == PlotSize.FourMarla) continue;
            if (pType == PlotType.Commercial && pSize == PlotSize.TwoKanal) continue;

            var baseAmt = pSize switch
            {
                PlotSize.FourMarla  => 30_000m,
                PlotSize.FiveMarla  => 40_000m,
                PlotSize.EightMarla => 60_000m,
                PlotSize.TenMarla   => 85_000m,
                PlotSize.OneKanal   => 140_000m,
                PlotSize.TwoKanal   => 230_000m,
                _                   => 40_000m
            };
            var mult = tier switch { PackageTier.Bronze => 1.0m, PackageTier.Silver => 1.5m, _ => 2.2m };
            var pkg = new Package { PlotType = pType, PlotSize = pSize, PackageTier = tier,
                PackageCategory = PackageCategory.Supervision, DesignType = DesignType.DdfcInclusive, IsActive = true };
            int n = 1;
            pkg.LineItems.Add(Li("Site Supervision Visits",           baseAmt * mult * 0.25m, false, n++));
            pkg.LineItems.Add(Li("Construction Progress Reports",      baseAmt * mult * 0.20m, false, n++));
            pkg.LineItems.Add(Li("Quality Control Checks",             baseAmt * mult * 0.20m, false, n++));
            pkg.LineItems.Add(Li("Site Documentation & Photography",   baseAmt * mult * 0.10m, false, n++));
            if (tier >= PackageTier.Silver)
            {
                pkg.LineItems.Add(Li("Weekly Site Visits",             baseAmt * mult * 0.15m, false, n++));
                pkg.LineItems.Add(Li("Structural Compliance Monitoring", baseAmt * mult * 0.10m, false, n++));
            }
            if (tier == PackageTier.Gold)
            {
                pkg.LineItems.Add(Li("Daily Supervision",              baseAmt * mult * 0.20m, false, n++));
                pkg.LineItems.Add(Li("Completion Certificate Assistance", baseAmt * mult * 0.10m, false, n++));
            }
            db.Packages.Add(pkg);
        }
    }

    private static PackageLineItem Li(string name, decimal amount, bool isFree, int order) => new()
    {
        ServiceName     = name,
        AmountDDFC      = isFree ? 0m : amount,
        AmountExclusive = isFree ? 0m : amount * 1.15m,
        IsFree          = isFree,
        SortOrder       = order,
    };

    // ══════════════════════════════════════════════════════════════════════════
    // ── Plots (DHA Peshawar Phase 1 representative sample) ────────────────────
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task SeedPlotsAsync(DDFCDbContext db)
    {
        if (await db.Plots.AnyAsync()) return;

        var plots = new List<Plot>();

        // ── Sector A – Residential ───────────────────────────────────────────
        plots.AddRange(new[]
        {
            P("001-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Road",     "Plot A-002", "Plot B-001", "Street 1"),
            P("002-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot A-001","Plot A-003", "Plot B-002", "Street 1"),
            P("003-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot A-002","Plot A-004", "Plot B-003", "Street 1"),
            P("004-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot A-003","Park",       "Plot B-004", "Street 1"),
            P("005-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Road",     "Plot A-006", "Park",       "Street 2"),
            P("006-A", "A", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot A-005","Plot A-007", "Park",       "Street 2"),
            P("007-A", "A", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Road",     "Plot A-008", "Plot C-007", "Street 3"),
            P("008-A", "A", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot A-007","Plot A-009", "Plot C-008", "Street 3"),
            P("009-A", "A", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot A-008","Park",       "Plot C-009", "Street 3"),
            P("010-A", "A", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Road",     "Plot A-011", "Plot D-010", "Main Boulevard"),
            P("011-A", "A", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Plot A-010","Plot A-012", "Plot D-011", "Main Boulevard"),
            P("012-A", "A", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Plot A-011","Open Space", "Plot D-012", "Main Boulevard"),
            P("013-A", "A", "4",  "1", PlotSize.TwoKanal,   PlotType.Residential, 70m, 70m, 55m, 55m, "Road",     "Plot A-014", "Masjid",     "Main Boulevard"),
            P("014-A", "A", "4",  "1", PlotSize.TwoKanal,   PlotType.Residential, 70m, 70m, 55m, 55m, "Plot A-013","Open Space", "Masjid",     "Main Boulevard"),
        });

        // ── Sector B – Residential ───────────────────────────────────────────
        plots.AddRange(new[]
        {
            P("001-B", "B", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Road",      "Plot B-002", "Plot A-001", "Street 5"),
            P("002-B", "B", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot B-001","Plot B-003", "Plot A-002", "Street 5"),
            P("003-B", "B", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot B-002","Plot B-004", "Plot A-003", "Street 5"),
            P("004-B", "B", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot B-003","Park",       "Plot A-004", "Street 5"),
            P("005-B", "B", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Road",      "Plot B-006", "Open Space", "Street 6"),
            P("006-B", "B", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot B-005","Plot B-007", "Open Space", "Street 6"),
            P("007-B", "B", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot B-006","Park",       "Road",       "Street 6"),
            P("008-B", "B", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Road",      "Plot B-009", "Plot E-008", "Sector Road"),
            P("009-B", "B", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Plot B-008","Plot B-010", "Plot E-009", "Sector Road"),
            P("010-B", "B", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Plot B-009","Open Space", "Plot E-010", "Sector Road"),
        });

        // ── Sector C – Residential ───────────────────────────────────────────
        plots.AddRange(new[]
        {
            P("001-C", "C", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Road",      "Plot C-002", "Plot F-001", "Street 9"),
            P("002-C", "C", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot C-001","Plot C-003", "Plot F-002", "Street 9"),
            P("003-C", "C", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot C-002","Park",       "Plot F-003", "Street 9"),
            P("004-C", "C", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Road",      "Plot C-005", "Road",       "Street 10"),
            P("005-C", "C", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot C-004","Plot C-006", "Road",       "Street 10"),
            P("006-C", "C", "2",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot C-005","Park",       "Road",       "Street 10"),
            P("007-C", "C", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Road",      "Plot C-008", "Plot G-007", "Sector Road"),
            P("008-C", "C", "3",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Plot C-007","Open Space", "Plot G-008", "Sector Road"),
            P("009-C", "C", "4",  "1", PlotSize.TwoKanal,   PlotType.Residential, 70m, 70m, 55m, 55m, "Road",      "Plot C-010", "Open Space", "Main Boulevard"),
            P("010-C", "C", "4",  "1", PlotSize.TwoKanal,   PlotType.Residential, 70m, 70m, 55m, 55m, "Plot C-009","Park",       "Open Space", "Main Boulevard"),
        });

        // ── Sector D – Mixed (Residential & Commercial) ──────────────────────
        plots.AddRange(new[]
        {
            P("001-D", "D", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Road",      "Plot D-002", "Road",       "Street 12"),
            P("002-D", "D", "1",  "1", PlotSize.FiveMarla,  PlotType.Residential, 25m, 25m, 20m, 20m, "Plot D-001","Plot D-003", "Road",       "Street 12"),
            P("003-D", "D", "1",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot D-002","Plot D-004", "Park",       "Street 12"),
            P("004-D", "D", "1",  "1", PlotSize.TenMarla,   PlotType.Residential, 35m, 35m, 25m, 25m, "Plot D-003","Plot D-005", "Park",       "Street 12"),
            P("005-D", "D", "2",  "1", PlotSize.OneKanal,   PlotType.Residential, 50m, 50m, 40m, 40m, "Road",      "Plot D-006", "Open Space", "Main Boulevard"),
            P("006-D", "D", "2",  "1", PlotSize.TwoKanal,   PlotType.Residential, 70m, 70m, 55m, 55m, "Plot D-005","Open Space", "Masjid",     "Main Boulevard"),
            // Commercial plots in Sector D
            P("C-01-D","D", "3",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "Road",      "C-02-D",    "Road",       "Commercial Strip"),
            P("C-02-D","D", "3",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "C-01-D",    "C-03-D",    "Road",       "Commercial Strip"),
            P("C-03-D","D", "3",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "C-02-D",    "C-04-D",    "Road",       "Commercial Strip"),
            P("C-04-D","D", "3",  "1", PlotSize.EightMarla, PlotType.Commercial,  30m, 30m, 25m, 25m, "C-03-D",    "C-05-D",    "Road",       "Commercial Strip"),
            P("C-05-D","D", "3",  "1", PlotSize.EightMarla, PlotType.Commercial,  30m, 30m, 25m, 25m, "C-04-D",    "Open Space","Road",       "Commercial Strip"),
        });

        // ── Sector E – Commercial Zone ───────────────────────────────────────
        plots.AddRange(new[]
        {
            P("C-01-E","E", "1",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "Road",      "C-02-E",    "Road",       "Main Commercial Road"),
            P("C-02-E","E", "1",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "C-01-E",    "C-03-E",    "Road",       "Main Commercial Road"),
            P("C-03-E","E", "1",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "C-02-E",    "C-04-E",    "Road",       "Main Commercial Road"),
            P("C-04-E","E", "1",  "1", PlotSize.FourMarla,  PlotType.Commercial,  20m, 20m, 18m, 18m, "C-03-E",    "C-05-E",    "Road",       "Main Commercial Road"),
            P("C-05-E","E", "2",  "1", PlotSize.EightMarla, PlotType.Commercial,  30m, 30m, 25m, 25m, "Road",      "C-06-E",    "Open Space", "Main Commercial Road"),
            P("C-06-E","E", "2",  "1", PlotSize.EightMarla, PlotType.Commercial,  30m, 30m, 25m, 25m, "C-05-E",    "C-07-E",    "Open Space", "Main Commercial Road"),
            P("C-07-E","E", "2",  "1", PlotSize.EightMarla, PlotType.Commercial,  30m, 30m, 25m, 25m, "C-06-E",    "C-08-E",    "Road",       "Main Commercial Road"),
            P("C-08-E","E", "3",  "1", PlotSize.OneKanal,   PlotType.Commercial,  50m, 50m, 40m, 40m, "Road",      "C-09-E",    "Road",       "Main Commercial Road"),
            P("C-09-E","E", "3",  "1", PlotSize.OneKanal,   PlotType.Commercial,  50m, 50m, 40m, 40m, "C-08-E",    "Open Space","Road",       "Main Commercial Road"),
            P("C-10-E","E", "3",  "1", PlotSize.OneKanal,   PlotType.Commercial,  50m, 50m, 40m, 40m, "C-09-E",    "Park",      "Road",       "Main Commercial Road"),
        });

        db.Plots.AddRange(plots);
        await db.SaveChangesAsync();
    }

    private static Plot P(
        string number, string sector, string street, string phase,
        PlotSize size, PlotType type,
        decimal l1, decimal l2, decimal s1, decimal s2,
        string north, string south, string east, string west) => new()
    {
        PlotNumber    = number,
        SectorNo      = sector,
        StreetNo      = street,
        PhaseNo       = phase,
        PlotSize      = size,
        PlotType      = type,
        CurrentStatus = PlotStatus.Available,
        LongerSide1   = l1,
        LongerSide2   = l2,
        ShorterSide1  = s1,
        ShorterSide2  = s2,
        BoundedNorth  = north,
        BoundedSouth  = south,
        BoundedEast   = east,
        BoundedWest   = west,
    };

    // ══════════════════════════════════════════════════════════════════════════
    // ── Demo Customers ────────────────────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task SeedCustomersAsync(DDFCDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;

        db.Customers.AddRange(
            new Customer { FullName = "Muhammad Rizwan Khan",   CNIC = "17301-1234567-1", PhoneNumber = "0300-1234567", Email = "rizwan.khan@gmail.com",    Address = "House 12, Street 3, Hayatabad, Peshawar" },
            new Customer { FullName = "Amina Bibi",             CNIC = "17301-2345678-2", PhoneNumber = "0301-2345678", Email = "amina.bibi@yahoo.com",      Address = "Flat 4, Block C, University Town, Peshawar" },
            new Customer { FullName = "Tariq Mehmood",          CNIC = "17301-3456789-3", PhoneNumber = "0302-3456789", Email = "tariq.m@hotmail.com",       Address = "Qila Road, Peshawar Cantonment" },
            new Customer { FullName = "Sadia Noor",             CNIC = "17301-4567890-4", PhoneNumber = "0303-4567890", Email = "sadia.noor@gmail.com",      Address = "House 88, Sector F6, Islamabad" },
            new Customer { FullName = "Khalid Hussain",         CNIC = "17301-5678901-5", PhoneNumber = "0304-5678901", Email = "khalid.h@outlook.com",      Address = "Village Chamkani, Peshawar" },
            new Customer { FullName = "Rukhsana Begum",         CNIC = "17301-6789012-6", PhoneNumber = "0305-6789012", Email = null,                        Address = "Khyber Road, Peshawar" },
            new Customer { FullName = "Asad Ullah",             CNIC = "17301-7890123-7", PhoneNumber = "0306-7890123", Email = "asad.ullah@gmail.com",      Address = "Ring Road, Peshawar" },
            new Customer { FullName = "Nadia Perveen",          CNIC = "17301-8901234-8", PhoneNumber = "0307-8901234", Email = "nadia.p@yahoo.com",         Address = "House 5, DHA Phase 1, Peshawar" },
            new Customer { FullName = "Zahid Anwar",            CNIC = "17301-9012345-9", PhoneNumber = "0308-9012345", Email = "zahid.anwar@gmail.com",     Address = "Model Town, Peshawar" },
            new Customer { FullName = "Farrukh Niaz",           CNIC = "17301-0123456-0", PhoneNumber = "0309-0123456", Email = "farrukh.n@gmail.com",       Address = "Gulbahar, Peshawar" }
        );
        await db.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ── Helper Methods ────────────────────────────────────────────────────────
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds multiple claims to a role
    /// </summary>
    private static async Task AddRoleClaims(RoleManager<Role> roleManager, Role role, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            await roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }
    }

    /// <summary>
    /// Creates a user and assigns role and custom claims
    /// </summary>
    private static async Task CreateUserWithClaims(
        UserManager<User> userManager,
        string fullName, 
        string email, 
        string password,
        Guid departmentId, 
        string roleName,
        (string Type, string Value)[] claims)
    {
        var user = new User
        {
            UserName       = email,   // Identity requires UserName; we use Email as UserName
            Email          = email,
            FullName       = fullName,
            DepartmentId   = departmentId,
            IsActive       = true,
            IsAvailable    = true,
            EmailConfirmed = true,  // skip confirmation for internal staff accounts
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            // Add user to role
            await userManager.AddToRoleAsync(user, roleName);

            // Add custom claims
            var claimsList = claims.Select(c => new Claim(c.Type, c.Value)).ToList();
            await userManager.AddClaimsAsync(user, claimsList);
        }
    }
}
