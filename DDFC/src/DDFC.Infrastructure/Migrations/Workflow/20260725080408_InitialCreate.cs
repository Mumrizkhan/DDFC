using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.Workflow
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "Processes",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessSteps",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ActionCompletionMode = table.Column<int>(type: "int", nullable: false),
                    MinimumActionCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessSteps_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "workflow",
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requests_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "workflow",
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepActions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    IsRejectionAction = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepActions_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalSchema: "workflow",
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepTransitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    ParallelCompletionMode = table.Column<int>(type: "int", nullable: false),
                    MinimumCompletionCount = table.Column<int>(type: "int", nullable: true),
                    IsBackwardTransition = table.Column<bool>(type: "bit", nullable: false),
                    ResetTargetStepOnReturn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepTransitions_ProcessSteps_FromStepId",
                        column: x => x.FromStepId,
                        principalSchema: "workflow",
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StepTransitions_ProcessSteps_ToStepId",
                        column: x => x.ToStepId,
                        principalSchema: "workflow",
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestSteps",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParallelGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParallelCompletionMode = table.Column<int>(type: "int", nullable: false),
                    MinimumCompletionCount = table.Column<int>(type: "int", nullable: true),
                    ActionCompletionMode = table.Column<int>(type: "int", nullable: false),
                    MinimumActionCount = table.Column<int>(type: "int", nullable: true),
                    PreviousStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivationCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestSteps_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalSchema: "workflow",
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestSteps_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "workflow",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestActions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestActions_RequestSteps_RequestStepId",
                        column: x => x.RequestStepId,
                        principalSchema: "workflow",
                        principalTable: "RequestSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestActions_StepActions_StepActionId",
                        column: x => x.StepActionId,
                        principalSchema: "workflow",
                        principalTable: "StepActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_ProcessId",
                schema: "workflow",
                table: "ProcessSteps",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestActions_RequestStepId",
                schema: "workflow",
                table: "RequestActions",
                column: "RequestStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestActions_StepActionId",
                schema: "workflow",
                table: "RequestActions",
                column: "StepActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ProcessId",
                schema: "workflow",
                table: "Requests",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSteps_ProcessStepId",
                schema: "workflow",
                table: "RequestSteps",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSteps_RequestId",
                schema: "workflow",
                table: "RequestSteps",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StepActions_ProcessStepId",
                schema: "workflow",
                table: "StepActions",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepTransitions_FromStepId",
                schema: "workflow",
                table: "StepTransitions",
                column: "FromStepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepTransitions_ToStepId",
                schema: "workflow",
                table: "StepTransitions",
                column: "ToStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestActions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "StepTransitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "RequestSteps",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "StepActions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Requests",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessSteps",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Processes",
                schema: "workflow");
        }
    }
}
