using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPackagePermitJsaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "WorkPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JsaRequired",
                table: "WorkPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JsaStatus",
                table: "WorkPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "WorkPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitNumber",
                table: "WorkPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PermitRequired",
                table: "WorkPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermitStatus",
                table: "WorkPackages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "JsaRequired",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "JsaStatus",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "PermitNumber",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "PermitRequired",
                table: "WorkPackages");

            migrationBuilder.DropColumn(
                name: "PermitStatus",
                table: "WorkPackages");
        }
    }
}
