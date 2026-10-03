using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.Workflow
{
    /// <inheritdoc />
    public partial class AddIsLoopbackActionToStepAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoopbackAction",
                schema: "workflow",
                table: "StepActions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoopbackAction",
                schema: "workflow",
                table: "StepActions");
        }
    }
}
