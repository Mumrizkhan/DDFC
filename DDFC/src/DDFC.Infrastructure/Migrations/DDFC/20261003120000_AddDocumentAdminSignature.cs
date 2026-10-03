using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDFC.Infrastructure.Migrations.DDFC
{
    [DbContext(typeof(DDFCDbContext))]
    [Migration("20261003120000_AddDocumentAdminSignature")]
    public class AddDocumentAdminSignature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSignedByAdmin",
                schema: "ddfc",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSignedByAdmin",
                schema: "ddfc",
                table: "Documents");
        }
    }
}