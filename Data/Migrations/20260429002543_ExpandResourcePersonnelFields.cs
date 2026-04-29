using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandResourcePersonnelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentStatus",
                table: "Resources",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "ShiftEligibility",
                table: "Resources",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Any");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Resources",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Resources",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CompanyCode_EmployeeId",
                table: "Resources",
                columns: new[] { "CompanyCode", "EmployeeId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_CompanyCode_EmployeeId",
                table: "Resources");

            migrationBuilder.DropColumn(name: "EmployeeId", table: "Resources");
            migrationBuilder.DropColumn(name: "FirstName", table: "Resources");
            migrationBuilder.DropColumn(name: "LastName", table: "Resources");
            migrationBuilder.DropColumn(name: "Region", table: "Resources");
            migrationBuilder.DropColumn(name: "EmploymentStatus", table: "Resources");
            migrationBuilder.DropColumn(name: "ShiftEligibility", table: "Resources");
            migrationBuilder.DropColumn(name: "Phone", table: "Resources");
            migrationBuilder.DropColumn(name: "Email", table: "Resources");
            migrationBuilder.DropColumn(name: "Notes", table: "Resources");
        }
    }
}
