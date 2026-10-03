using DDFC.API.Debugging;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.StaticFiles;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Infrastructure.Data;
using DDFC.Infrastructure.Persistence;
using DDFC.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Services;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var cfg     = builder.Configuration;

// ── Database Contexts ─────────────────────────────────────────────────────────
var connStr = cfg.GetConnectionString("Default")!;

builder.Services.AddDbContext<DDFCDbContext>(o =>
    o.UseSqlServer(connStr));

builder.Services.AddDbContext<WorkflowDbContext>(o =>
    o.UseSqlServer(connStr, b => b.MigrationsAssembly("DDFC.Infrastructure")));

// ── WorkflowEngine DI ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IWorkflowEngine,     WorkflowEngineService>();

// ── ASP.NET Core Identity (UserManager / RoleManager) ───────────────────────
builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength          = 8;
        options.Password.RequireUppercase        = true;
        options.Password.RequireNonAlphanumeric  = false;
        options.User.RequireUniqueEmail          = true;
        options.SignIn.RequireConfirmedEmail     = false;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<DDFCDbContext>()
    .AddDefaultTokenProviders();

// ── DDFC Application Services ─────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISmsService,              TwilioSmsService>();
builder.Services.AddScoped<IEmailService,               SmtpEmailService>();
builder.Services.AddSingleton<IOtpService,             OtpService>();
builder.Services.AddScoped<IJwtService,                JwtService>();
builder.Services.AddScoped<IRoundRobinService,         RoundRobinService>();
builder.Services.AddScoped<ITaskAssignmentService,     TaskAssignmentService>();
builder.Services.AddScoped<INotificationService,       NotificationService>();
builder.Services.AddScoped<IPackageService,            PackageService>();
builder.Services.AddScoped<ITicketService,             TicketService>();
builder.Services.AddScoped<IPossessionRequestService,  PossessionRequestService>();

// ── JWT Authentication ─────────────────────────────────────────────────────────
var jwtKey = cfg["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = true,
            ValidIssuer              = cfg["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = cfg["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(o =>
{
    // ═══════════════════════════════════════════════════════════════════════
    // Basic User Type Policies
    // ═══════════════════════════════════════════════════════════════════════
    o.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
    o.AddPolicy("StaffOrAdmin", p => p.RequireAssertion(c =>
        c.User.HasClaim("userType", "staff") || c.User.HasClaim("role", "Admin")));
    o.AddPolicy("CustomerOnly", p => p.RequireClaim("userType", "customer"));
    o.AddPolicy("StaffOnly", p => p.RequireClaim("userType", "staff"));

    // ═══════════════════════════════════════════════════════════════════════
    // Role-Based Policies (for backward compatibility)
    // ═══════════════════════════════════════════════════════════════════════
    o.AddPolicy("ReceptionOfficer", p => p.RequireClaim("role", "Reception Officer"));
    o.AddPolicy("TransferOfficer", p => p.RequireClaim("role", "Transfer Officer"));
    o.AddPolicy("FinanceOfficer", p => p.RequireClaim("role", "Finance Officer"));
    o.AddPolicy("BuildingControlOfficer", p => p.RequireClaim("role", "Building Control Officer"));
    o.AddPolicy("Architect", p => p.RequireClaim("role", "Architect"));
    o.AddPolicy("StructureEngineer", p => p.RequireClaim("role", "Structure Engineer"));
    o.AddPolicy("MEPEngineer", p => p.RequireClaim("role", "MEP Engineer"));
    o.AddPolicy("PrincipalArchitect", p => p.RequireClaim("role", "Principal Architect"));
    o.AddPolicy("DesignHead", p => p.RequireClaim("role", "DHA Design Head"));
    o.AddPolicy("TechnicalSupport", p => p.RequireClaim("role", "Technical Support"));
    o.AddPolicy("PossessionAdmin", p => p.RequireClaim("role", "Possession Admin"));

    // ═══════════════════════════════════════════════════════════════════════
    // Permission-Based Policies (Role Claims) - RECOMMENDED APPROACH
    // ═══════════════════════════════════════════════════════════════════════
    
    // ── Admin Permissions ──────────────────────────────────────────────────
    o.AddPolicy("CanManageUsers", p => p.RequireClaim("permission", "CanManageUsers"));
    o.AddPolicy("CanManageCustomers", p => p.RequireClaim("permission", "CanManageCustomers"));
    o.AddPolicy("CanConfigurePackages", p => p.RequireClaim("permission", "CanConfigurePackages"));
    o.AddPolicy("CanViewAllRequests", p => p.RequireClaim("permission", "CanViewAllRequests"));
    o.AddPolicy("CanViewReports", p => p.RequireClaim("permission", "CanViewReports"));
    o.AddPolicy("CanManageTemplates", p => p.RequireClaim("permission", "CanManageTemplates"));
    o.AddPolicy("CanManageDepartments", p => p.RequireClaim("permission", "CanManageDepartments"));
    o.AddPolicy("CanManageRoles", p => p.RequireClaim("permission", "CanManageRoles"));
    o.AddPolicy("CanManageSettings", p => p.RequireClaim("permission", "CanManageSettings"));
    o.AddPolicy("CanResetRoundRobin", p => p.RequireClaim("permission", "CanResetRoundRobin"));

    // ── Reception Permissions ──────────────────────────────────────────────
    o.AddPolicy("CanCreateRequest", p => p.RequireClaim("permission", "CanCreateRequest"));
    o.AddPolicy("CanSelectPackage", p => p.RequireClaim("permission", "CanSelectPackage"));
    o.AddPolicy("CanDeliverDocuments", p => p.RequireClaim("permission", "CanDeliverDocuments"));

    // ── Possession Admin Permissions ───────────────────────────────────────
    o.AddPolicy("CanAdminReview", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanAdminReview") ||
        c.User.HasClaim("role", "Admin")));

    // ── Transfer & Finance Permissions ─────────────────────────────────────
    o.AddPolicy("CanApproveTransfer", p => p.RequireClaim("permission", "CanApproveTransfer"));
    o.AddPolicy("CanApproveFinance", p => p.RequireClaim("permission", "CanApproveFinance"));
    o.AddPolicy("CanConfirmPayment", p => p.RequireClaim("permission", "CanConfirmPayment"));

    // AD Coordinator review — runs after Finance, before DDFC Admin signs
    o.AddPolicy("CanApproveAdCoord", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanApproveAdCoord") ||
        c.User.HasClaim("role", "AD Coordinator")));

    // Reception Officer OR anyone with CanConfirmPayment permission
    o.AddPolicy("CanConfirmPaymentOrReception", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanConfirmPayment") ||
        c.User.HasClaim("role", "Reception Officer")));

    // Possession Admin approves the challan Reception uploaded
    o.AddPolicy("CanApprovePayment", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanConfirmPayment") ||
        c.User.HasClaim("role", "Admin") ||
        c.User.HasClaim("role", "Possession Admin") ||
        c.User.HasClaim("role", "DDFC Admin")));

    // ── Planning & Control Permissions ─────────────────────────────────────
    o.AddPolicy("CanSubmitBuildingControl", p => p.RequireClaim("permission", "CanSubmitBuildingControl"));
    o.AddPolicy("CanUploadSoilTest", p => p.RequireClaim("permission", "CanUploadSoilTest"));
    o.AddPolicy("CanIssuePossessionCert", p => p.RequireClaim("permission", "CanIssuePossessionCert"));
    o.AddPolicy("CanSignPossessionLetter", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanSignPossessionLetter") ||
        c.User.HasClaim("role", "Admin") ||
        c.User.HasClaim("role", "DDFC Admin") ||
        c.User.HasClaim("role", "Possession Admin")));

    // ── Design & Engineering Permissions ───────────────────────────────────
    o.AddPolicy("CanUploadPlan", p => p.RequireClaim("permission", "CanUploadPlan"));
    o.AddPolicy("CanComplete3D", p => p.RequireClaim("permission", "CanComplete3D"));
    o.AddPolicy("CanUpload3D", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanComplete3D") ||
        c.User.HasClaim("permission", "CanAssign3DOperator")));
    o.AddPolicy("CanAssign3DOperator", p => p.RequireClaim("permission", "CanAssign3DOperator"));
    o.AddPolicy("CanCompleteStructure", p => p.RequireClaim("permission", "CanCompleteStructure"));
    o.AddPolicy("CanCompleteMEP", p => p.RequireClaim("permission", "CanCompleteMEP"));
    o.AddPolicy("CanPrincipalApprove", p => p.RequireClaim("permission", "CanPrincipalApprove"));
    o.AddPolicy("CanFinalApprove", p => p.RequireClaim("permission", "CanFinalApprove"));

    // ── Support Permissions ────────────────────────────────────────────────
    o.AddPolicy("CanManageTickets", p => p.RequireClaim("permission", "CanManageTickets"));
    o.AddPolicy("CanReplyTickets", p => p.RequireClaim("permission", "CanReplyTickets"));
    o.AddPolicy("CanResolveTickets", p => p.RequireClaim("permission", "CanResolveTickets"));
    o.AddPolicy("CanReopenTickets", p => p.RequireClaim("permission", "CanReopenTickets"));

    // ═══════════════════════════════════════════════════════════════════════
    // User Claim-Based Policies (User-Specific Attributes)
    // ═══════════════════════════════════════════════════════════════════════
    
    // ── Approval Authorities ───────────────────────────────────────────────
    o.AddPolicy("HasApprovalAuthority", p => p.RequireAssertion(c =>
        c.User.HasClaim(claim => claim.Type == "approvalAuthority")));
    
    o.AddPolicy("TransferApprovalAuthority", p => 
        p.RequireClaim("approvalAuthority", "Transfer"));
    
    o.AddPolicy("FinanceApprovalAuthority", p => 
        p.RequireClaim("approvalAuthority", "Finance"));

    // ── Engineering Licenses ───────────────────────────────────────────────
    o.AddPolicy("HasEngineeringLicense", p => p.RequireAssertion(c =>
        c.User.HasClaim(claim => claim.Type == "engineeringLicense")));

    // ── Senior Authorities ─────────────────────────────────────────────────
    o.AddPolicy("SeniorApprovalAuthority", p => 
        p.RequireClaim("seniorApprovalAuthority", "true"));
    
    o.AddPolicy("FinalApprovalAuthority", p => 
        p.RequireClaim("finalApprovalAuthority", "true"));

    // ── Inspection Authority ───────────────────────────────────────────────
    o.AddPolicy("InspectionAuthority", p => 
        p.RequireClaim("inspectionAuthority", "true"));

    // ── Department-Specific ────────────────────────────────────────────────
    o.AddPolicy("FrontDeskDepartment", p => p.RequireClaim("departmentCode", "FD"));
    o.AddPolicy("TransferDepartment", p => p.RequireClaim("departmentCode", "TB"));
    o.AddPolicy("FinanceDepartment", p => p.RequireClaim("departmentCode", "FB"));
    o.AddPolicy("BuildingControlDepartment", p => p.RequireClaim("departmentCode", "BC"));
    o.AddPolicy("ArchitectureDepartment", p => p.RequireClaim("departmentCode", "AD"));
    o.AddPolicy("ThreeDDepartment", p => p.RequireClaim("departmentCode", "3D"));
    o.AddPolicy("StructureDepartment", p => p.RequireClaim("departmentCode", "SD"));
    o.AddPolicy("MEPDepartment", p => p.RequireClaim("departmentCode", "MD"));
    o.AddPolicy("PrincipalOfficeDepartment", p => p.RequireClaim("departmentCode", "PO"));
    o.AddPolicy("DHADesignDepartment", p => p.RequireClaim("departmentCode", "DD"));
    o.AddPolicy("AdministrationDepartment", p => p.RequireClaim("departmentCode", "AS"));
    o.AddPolicy("TechnicalSupportDepartment", p => p.RequireClaim("departmentCode", "TS"));

    // ═══════════════════════════════════════════════════════════════════════
    // Combined/Complex Policies
    // ═══════════════════════════════════════════════════════════════════════
    
    // ── Senior Architects (Principal or Design Head) ───────────────────────
    o.AddPolicy("SeniorArchitect", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanPrincipalApprove") ||
        c.User.HasClaim("permission", "CanFinalApprove")));

    // ── Licensed Engineers (Structure or MEP with license) ─────────────────
    o.AddPolicy("LicensedEngineer", p => p.RequireAssertion(c =>
        (c.User.HasClaim("permission", "CanCompleteStructure") ||
         c.User.HasClaim("permission", "CanCompleteMEP")) &&
        c.User.HasClaim(claim => claim.Type == "engineeringLicense")));

    // ── Design Specialists (Architects + Engineers) ────────────────────────
    o.AddPolicy("DesignSpecialist", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanUploadPlan") ||
        c.User.HasClaim("permission", "CanCompleteStructure") ||
        c.User.HasClaim("permission", "CanCompleteMEP")));

    // ── Approval Chain (Any approval permission) ───────────────────────────
    o.AddPolicy("CanApprove", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanApproveTransfer") ||
        c.User.HasClaim("permission", "CanApproveFinance") ||
        c.User.HasClaim("permission", "CanPrincipalApprove") ||
        c.User.HasClaim("permission", "CanFinalApprove")));

    // ── Payment Handlers ───────────────────────────────────────────────────
    o.AddPolicy("CanHandlePayments", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanConfirmPayment") ||
        c.User.HasClaim("canVerifyPayments", "true")));

    // ── Request Initiators ─────────────────────────────────────────────────
    o.AddPolicy("CanInitiateRequests", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "CanCreateRequest") ||
        c.User.HasClaim("canInitiateRequests", "true")));

    // ── Technical Team (Engineers + Architects) ────────────────────────────
    o.AddPolicy("TechnicalTeam", p => p.RequireAssertion(c =>
        c.User.HasClaim("departmentCode", "AD") ||
        c.User.HasClaim("departmentCode", "3D") ||
        c.User.HasClaim("departmentCode", "SD") ||
        c.User.HasClaim("departmentCode", "MD") ||
        c.User.HasClaim("departmentCode", "PO") ||
        c.User.HasClaim("departmentCode", "DD")));

    // ── Operational Team (Front desk, Transfer, Finance) ──────────────────
    o.AddPolicy("OperationalTeam", p => p.RequireAssertion(c =>
        c.User.HasClaim("departmentCode", "FD") ||
        c.User.HasClaim("departmentCode", "TB") ||
        c.User.HasClaim("departmentCode", "FB")));

    // ── Regulatory Team (Town Planning, Building Control) ─────────────────
    o.AddPolicy("RegulatoryTeam", p => p.RequireAssertion(c =>
        c.User.HasClaim("departmentCode", "TP") ||
        c.User.HasClaim("departmentCode", "BC")));

    // ── Experienced Staff (5+ years) ───────────────────────────────────────
    o.AddPolicy("ExperiencedStaff", p => p.RequireAssertion(c =>
    {
        var yearsExp = c.User.FindFirst("yearsOfExperience")?.Value;
        return int.TryParse(yearsExp, out int years) && years >= 5;
    }));

    // ── Super Admin (Admin with SuperAdmin access level) ──────────────────
    o.AddPolicy("SuperAdmin", p => p.RequireAssertion(c =>
        c.User.HasClaim("role", "Admin") &&
        c.User.HasClaim("accessLevel", "SuperAdmin")));

    // ── Admin and Reception Officer (Customer Management) ────────────────
    o.AddPolicy("CanManageCustomers", p => p.RequireAssertion(c =>
        // Admin role has full access
        c.User.HasClaim("role", "Admin") ||
        // Reception Officer has full access
        c.User.HasClaim("role", "Reception Officer") ||
        // Or anyone with staff type and the permission claim containing CanCreateRequest
        (c.User.HasClaim("userType", "staff") && 
         c.User.Claims.FirstOrDefault(cl => cl.Type == "permission")?.Value?.Contains("CanCreateRequest") == true)));
});

// Register debug authorization handler
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, DebugAuthorizationMiddlewareResultHandler>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DDFC API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Description  = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

// ── Startup: Migrate + Seed ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var ddfc = scope.ServiceProvider.GetRequiredService<DDFCDbContext>();
    var wf   = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

    await ddfc.Database.MigrateAsync();
    await wf.Database.MigrateAsync();

    var userManager   = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager   = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    await DDFCDataSeeder.SeedAsync(ddfc, userManager, roleManager);

    var processId            = await DDFCWorkflowSeeder.SeedAsync(scope.ServiceProvider);
    var revisedPlanProcessId = await RevisedPlanWorkflowSeeder.SeedAsync(scope.ServiceProvider);
    var asBuiltPlanProcessId = await AsBuiltPlanWorkflowSeeder.SeedAsync(scope.ServiceProvider);

    PossessionRequestService.SetDDFCProcessId(processId);
    PossessionRequestService.SetRevisedPlanProcessId(revisedPlanProcessId);
    PossessionRequestService.SetAsBuiltPlanProcessId(asBuiltPlanProcessId);
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DDFC API v1"));

app.UseCors();
app.UseHttpsRedirection();

// Serve uploaded files from the /uploads path
var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath  = "/uploads"
});

app.UseAuthentication();

// Debug middleware
app.UseMiddleware<DDFC.API.Debugging.AuthorizationDebugMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
