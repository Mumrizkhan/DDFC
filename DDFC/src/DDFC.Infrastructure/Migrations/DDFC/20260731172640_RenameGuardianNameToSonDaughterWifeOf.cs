using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.DDFC
{
    /// <inheritdoc />
    public partial class RenameGuardianNameToSonDaughterWifeOf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GuardianName",
                schema: "ddfc",
                table: "PossessionRequests",
                newName: "SonDaughterWifeOf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SonDaughterWifeOf",
                schema: "ddfc",
                table: "PossessionRequests",
                newName: "GuardianName");
        }
    }
}
