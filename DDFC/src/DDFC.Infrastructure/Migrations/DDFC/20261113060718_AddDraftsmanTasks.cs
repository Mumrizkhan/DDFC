using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.DDFC
{
    /// <inheritdoc />
    public partial class AddDraftsmanTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DraftsmanTasks",
                schema: "ddfc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentStep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftsmanTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DraftsmanTasks_PossessionRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "ddfc",
                        principalTable: "PossessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DraftsmanTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DraftsmanTasks_AssignedToUserId",
                schema: "ddfc",
                table: "DraftsmanTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftsmanTasks_RequestId_ParentStep",
                schema: "ddfc",
                table: "DraftsmanTasks",
                columns: new[] { "RequestId", "ParentStep" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftsmanTasks",
                schema: "ddfc");
        }
    }
}
