using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FcoDocuments",
                columns: table => new
                {
                    FcoDocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LinkedEstimateId = table.Column<int>(type: "int", nullable: true),
                    FcoNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreparedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientContact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractorContact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleImpactDays = table.Column<int>(type: "int", nullable: false),
                    RevisedCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LaborBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkupPct = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    TaxPct = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    TotalFcoAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedContractValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientApprovalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractorApprovalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractorApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevisionHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcoDocuments", x => x.FcoDocumentId);
                });

            migrationBuilder.CreateTable(
                name: "StepOutPlans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedEstimateId = table.Column<int>(type: "int", nullable: true),
                    LinkedStaffingPlanId = table.Column<int>(type: "int", nullable: true),
                    LinkedFcoDocumentId = table.Column<int>(type: "int", nullable: true),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Site = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepOutPlans", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "StepOutSteps",
                columns: table => new
                {
                    StepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    StepCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    RequiredPeople = table.Column<int>(type: "int", nullable: false),
                    CraftCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    PermitRequired = table.Column<bool>(type: "bit", nullable: false),
                    MaterialToolRequired = table.Column<bool>(type: "bit", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepOutSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_StepOutSteps_StepOutPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "StepOutPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkPackages",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CraftCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredPeople = table.Column<int>(type: "int", nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReadyForScheduling = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPackages", x => x.PackageId);
                    table.ForeignKey(
                        name: "FK_WorkPackages_StepOutPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "StepOutPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StepDependencies",
                columns: table => new
                {
                    DependencyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    PredecessorStepId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepDependencies", x => x.DependencyId);
                    table.ForeignKey(
                        name: "FK_StepDependencies_StepOutSteps_PredecessorStepId",
                        column: x => x.PredecessorStepId,
                        principalTable: "StepOutSteps",
                        principalColumn: "StepId");
                    table.ForeignKey(
                        name: "FK_StepDependencies_StepOutSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "StepOutSteps",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepResourceReqs",
                columns: table => new
                {
                    ReqId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    CraftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepResourceReqs", x => x.ReqId);
                    table.ForeignKey(
                        name: "FK_StepResourceReqs_StepOutSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "StepOutSteps",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepDependencies_PredecessorStepId",
                table: "StepDependencies",
                column: "PredecessorStepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepDependencies_StepId",
                table: "StepDependencies",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepOutSteps_PlanId",
                table: "StepOutSteps",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StepResourceReqs_StepId",
                table: "StepResourceReqs",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPackages_PlanId",
                table: "WorkPackages",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FcoDocuments");

            migrationBuilder.DropTable(
                name: "StepDependencies");

            migrationBuilder.DropTable(
                name: "StepResourceReqs");

            migrationBuilder.DropTable(
                name: "WorkPackages");

            migrationBuilder.DropTable(
                name: "StepOutSteps");

            migrationBuilder.DropTable(
                name: "StepOutPlans");
        }
    }
}
