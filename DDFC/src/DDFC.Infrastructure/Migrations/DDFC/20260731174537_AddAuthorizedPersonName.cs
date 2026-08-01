using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.DDFC
{
    /// <inheritdoc />
    public partial class AddAuthorizedPersonName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizedPersonName",
                schema: "ddfc",
                table: "PossessionRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorizedPersonName",
                schema: "ddfc",
                table: "PossessionRequests");
        }
    }
}
