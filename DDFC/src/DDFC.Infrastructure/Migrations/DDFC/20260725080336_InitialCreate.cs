using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.DDFC
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ddfc");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentOtp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PreferredSmsLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlotType = table.Column<int>(type: "int", nullable: false),
                    PlotSize = table.Column<int>(type: "int", nullable: false),
                    PackageTier = table.Column<int>(type: "int", nullable: false),
                    PackageCategory = table.Column<int>(type: "int", nullable: false),
                    DesignType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plots",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlotNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SectorNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhaseNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlotSize = table.Column<int>(type: "int", nullable: false),
                    PlotType = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    LongerSide1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LongerSide2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ShorterSide1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ShorterSide2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BoundedNorth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundedSouth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundedEast = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoundedWest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageLineItems",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountDDFC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountExclusive = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageLineItems_Packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "ddfc",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    NumberOfPersons = table.Column<int>(type: "int", nullable: false),
                    AttendeeNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingMethod = table.Column<int>(type: "int", nullable: false),
                    BookedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ddfc",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchitectUndertakings",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoldDays = table.Column<int>(type: "int", nullable: true),
                    HoldStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoldEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchitectUndertakings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchitecturalPlans",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchitecturalPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanRevisionRequests",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarkupFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanRevisionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanRevisionRequests_ArchitecturalPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "ddfc",
                        principalTable: "ArchitecturalPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CadAssignments",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CadType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CadAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CadFiles",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CadFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerNotifications",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresResponse = table.Column<bool>(type: "bit", nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerNotifications_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ddfc",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DelayUndertakings",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatedBy = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedByCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DelayReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedDelayDays = table.Column<int>(type: "int", nullable: true),
                    UndertakingDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelayUndertakings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastAssignedEmployeeIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "ddfc",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAvailabilities",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    UnavailableFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnavailableUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAvailabilities_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PossessionRequests",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MembershipDPRNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuardianRelation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contractor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllotmentLetterUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CnicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminRejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessageScreenshotUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EStampPaperUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizedPersonCnicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizedPersonPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferApproved = table.Column<bool>(type: "bit", nullable: false),
                    FinanceApproved = table.Column<bool>(type: "bit", nullable: false),
                    AdcAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StructureCompleted = table.Column<bool>(type: "bit", nullable: false),
                    MEPCompleted = table.Column<bool>(type: "bit", nullable: false),
                    SoilTestReportUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TownPlanningCompleted = table.Column<bool>(type: "bit", nullable: false),
                    BuildingControlCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CustomerApprovedPlan = table.Column<bool>(type: "bit", nullable: false),
                    AssignedArchitectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedInteriorDesignPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedSupervisionPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PossessionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ddfc",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Packages_SelectedInteriorDesignPackageId",
                        column: x => x.SelectedInteriorDesignPackageId,
                        principalSchema: "ddfc",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Packages_SelectedPackageId",
                        column: x => x.SelectedPackageId,
                        principalSchema: "ddfc",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Packages_SelectedSupervisionPackageId",
                        column: x => x.SelectedSupervisionPackageId,
                        principalSchema: "ddfc",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Plots_PlotId",
                        column: x => x.PlotId,
                        principalSchema: "ddfc",
                        principalTable: "Plots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PossessionRequests_Users_AssignedArchitectId",
                        column: x => x.AssignedArchitectId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEPReports",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEPReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MEPReports_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallanNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlotAnnexations",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdditionalArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnexationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlotAnnexations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlotAnnexations_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlotMergings",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MergedPlotNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MergedPlotSector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MergedPlotSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlotMergings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlotMergings_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PossessionCertificates",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemplateVersion = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HandedOverBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandedOverDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TakenOverBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TakenOverDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChiefSurveyorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdTpBcdName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PossessionCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PossessionCertificates_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestWorkflowHistories",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestWorkflowHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestWorkflowHistories_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestWorkflowHistories_Users_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SoilTestReports",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LabName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoilBearingCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoilTestReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoilTestReports_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructuralReports",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuralReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructuralReports_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SatisfactionRating = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ddfc",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Departments_AssignedDepartmentId",
                        column: x => x.AssignedDepartmentId,
                        principalSchema: "ddfc",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SurveyForms",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<int>(type: "int", nullable: false),
                    Responses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyForms_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyObservations",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Violations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Suggestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyObservations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "ddfc",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyObservations_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignments",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentMethod = table.Column<int>(type: "int", nullable: false),
                    ReassignedFromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReassignedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "ddfc",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ThreeDVisualizations",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreeDVisualizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreeDVisualizations_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentChallans",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallanNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentChallans_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "ddfc",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketReplies",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorType = table.Column<int>(type: "int", nullable: false),
                    MessageBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketReplies_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "ddfc",
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AssignedEmployeeId",
                schema: "ddfc",
                table: "Appointments",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BookedByUserId",
                schema: "ddfc",
                table: "Appointments",
                column: "BookedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                schema: "ddfc",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DepartmentId_AppointmentDate_StartTime",
                schema: "ddfc",
                table: "Appointments",
                columns: new[] { "DepartmentId", "AppointmentDate", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RequestId",
                schema: "ddfc",
                table: "Appointments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectUndertakings_RequestId",
                schema: "ddfc",
                table: "ArchitectUndertakings",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchitecturalPlans_RequestId",
                schema: "ddfc",
                table: "ArchitecturalPlans",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CadAssignments_AssignedUserId",
                schema: "ddfc",
                table: "CadAssignments",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CadAssignments_RequestId",
                schema: "ddfc",
                table: "CadAssignments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CadFiles_RequestId",
                schema: "ddfc",
                table: "CadFiles",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_CustomerId",
                schema: "ddfc",
                table: "CustomerNotifications",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_RequestId",
                schema: "ddfc",
                table: "CustomerNotifications",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CNIC",
                schema: "ddfc",
                table: "Customers",
                column: "CNIC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelayUndertakings_RequestId",
                schema: "ddfc",
                table: "DelayUndertakings",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadUserId",
                schema: "ddfc",
                table: "Departments",
                column: "HeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RequestId",
                schema: "ddfc",
                table: "Documents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAvailabilities_UserId",
                schema: "ddfc",
                table: "EmployeeAvailabilities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MEPReports_RequestId",
                schema: "ddfc",
                table: "MEPReports",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageLineItems_PackageId",
                schema: "ddfc",
                table: "PackageLineItems",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentChallans_PaymentId",
                schema: "ddfc",
                table: "PaymentChallans",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestId",
                schema: "ddfc",
                table: "Payments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRevisionRequests_PlanId",
                schema: "ddfc",
                table: "PlanRevisionRequests",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlotAnnexations_RequestId",
                schema: "ddfc",
                table: "PlotAnnexations",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlotMergings_RequestId",
                schema: "ddfc",
                table: "PlotMergings",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PossessionCertificates_RequestId",
                schema: "ddfc",
                table: "PossessionCertificates",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_AssignedArchitectId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "AssignedArchitectId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_CustomerId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_PlotId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "PlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_SelectedInteriorDesignPackageId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "SelectedInteriorDesignPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_SelectedPackageId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "SelectedPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_SelectedSupervisionPackageId",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "SelectedSupervisionPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PossessionRequests_Status",
                schema: "ddfc",
                table: "PossessionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RequestWorkflowHistories_ActionByUserId",
                schema: "ddfc",
                table: "RequestWorkflowHistories",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestWorkflowHistories_RequestId",
                schema: "ddfc",
                table: "RequestWorkflowHistories",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "identity",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SoilTestReports_RequestId",
                schema: "ddfc",
                table: "SoilTestReports",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StructuralReports_RequestId",
                schema: "ddfc",
                table: "StructuralReports",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedDepartmentId",
                schema: "ddfc",
                table: "SupportTickets",
                column: "AssignedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedUserId",
                schema: "ddfc",
                table: "SupportTickets",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CustomerId",
                schema: "ddfc",
                table: "SupportTickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_RequestId",
                schema: "ddfc",
                table: "SupportTickets",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyForms_RequestId",
                schema: "ddfc",
                table: "SurveyForms",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyObservations_DepartmentId",
                schema: "ddfc",
                table: "SurveyObservations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyObservations_RequestId",
                schema: "ddfc",
                table: "SurveyObservations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_AssignedToUserId",
                schema: "ddfc",
                table: "TaskAssignments",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_DepartmentId",
                schema: "ddfc",
                table: "TaskAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_RequestId",
                schema: "ddfc",
                table: "TaskAssignments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeDVisualizations_RequestId",
                schema: "ddfc",
                table: "ThreeDVisualizations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketReplies_TicketId",
                schema: "ddfc",
                table: "TicketReplies",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "identity",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "identity",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                schema: "identity",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Departments_DepartmentId",
                schema: "ddfc",
                table: "Appointments",
                column: "DepartmentId",
                principalSchema: "ddfc",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "Appointments",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_AssignedEmployeeId",
                schema: "ddfc",
                table: "Appointments",
                column: "AssignedEmployeeId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_BookedByUserId",
                schema: "ddfc",
                table: "Appointments",
                column: "BookedByUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ArchitectUndertakings_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "ArchitectUndertakings",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArchitecturalPlans_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "ArchitecturalPlans",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CadAssignments_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "CadAssignments",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CadAssignments_Users_AssignedUserId",
                schema: "ddfc",
                table: "CadAssignments",
                column: "AssignedUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CadFiles_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "CadFiles",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerNotifications_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "CustomerNotifications",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DelayUndertakings_PossessionRequests_RequestId",
                schema: "ddfc",
                table: "DelayUndertakings",
                column: "RequestId",
                principalSchema: "ddfc",
                principalTable: "PossessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_HeadUserId",
                schema: "ddfc",
                table: "Departments",
                column: "HeadUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "ArchitectUndertakings",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "CadAssignments",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "CadFiles",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "CustomerNotifications",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "DelayUndertakings",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "EmployeeAvailabilities",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "MEPReports",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PackageLineItems",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PaymentChallans",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PlanRevisionRequests",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PlotAnnexations",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PlotMergings",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "PossessionCertificates",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "RequestWorkflowHistories",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SoilTestReports",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "StructuralReports",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "SurveyForms",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "SurveyObservations",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "TaskAssignments",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Templates",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "ThreeDVisualizations",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "TicketReplies",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "ArchitecturalPlans",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "SupportTickets",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "PossessionRequests",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Packages",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Plots",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "ddfc");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");
        }
    }
}
