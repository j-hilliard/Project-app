using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProjectLifecycleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── StepOutStep: add ParentStepId + DurationHours ──────────────
            migrationBuilder.AddColumn<int>(
                name: "ParentStepId",
                table: "StepOutSteps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DurationHours",
                table: "StepOutSteps",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepOutSteps_ParentStepId",
                table: "StepOutSteps",
                column: "ParentStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_StepOutSteps_StepOutSteps_ParentStepId",
                table: "StepOutSteps",
                column: "ParentStepId",
                principalTable: "StepOutSteps",
                principalColumn: "StepId",
                onDelete: ReferentialAction.NoAction);

            // ── FcoDocument: add LinkedWorkOrderId ─────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "LinkedWorkOrderId",
                table: "FcoDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcoDocuments_LinkedWorkOrderId",
                table: "FcoDocuments",
                column: "LinkedWorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_FcoDocuments_WorkOrders_LinkedWorkOrderId",
                table: "FcoDocuments",
                column: "LinkedWorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.Restrict);

            // ── StepOutSubSteps ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "StepOutSubSteps",
                columns: table => new
                {
                    SubStepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    SubStepCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationHours = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    RequiredPeople = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CraftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDurationHours = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepOutSubSteps", x => x.SubStepId);
                    table.ForeignKey(
                        name: "FK_StepOutSubSteps_StepOutSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "StepOutSteps",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepOutSubSteps_StepId",
                table: "StepOutSubSteps",
                column: "StepId");

            // ── FcoLaborLines ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "FcoLaborLines",
                columns: table => new
                {
                    FcoLaborLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FcoDocumentId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LaborType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Direct"),
                    CraftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NavCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    OtHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DtHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    BillStRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BillOtRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BillDtRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcoLaborLines", x => x.FcoLaborLineId);
                    table.ForeignKey(
                        name: "FK_FcoLaborLines_FcoDocuments_FcoDocumentId",
                        column: x => x.FcoDocumentId,
                        principalTable: "FcoDocuments",
                        principalColumn: "FcoDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcoLaborLines_FcoDocumentId",
                table: "FcoLaborLines",
                column: "FcoDocumentId");

            // ── ActualEntries ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ActualEntries",
                columns: table => new
                {
                    ActualEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    FcoDocumentId = table.Column<int>(type: "int", nullable: true),
                    PlanTaskId = table.Column<int>(type: "int", nullable: true),
                    StepOutStepId = table.Column<int>(type: "int", nullable: true),
                    StepOutSubStepId = table.Column<int>(type: "int", nullable: true),
                    ActualType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Labor"),
                    ActualDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CraftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    OtHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DtHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BilledAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EnteredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualEntries", x => x.ActualEntryId);
                    table.ForeignKey(
                        name: "FK_ActualEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActualEntries_FcoDocuments_FcoDocumentId",
                        column: x => x.FcoDocumentId,
                        principalTable: "FcoDocuments",
                        principalColumn: "FcoDocumentId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ActualEntries_PlanTasks_PlanTaskId",
                        column: x => x.PlanTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ActualEntries_StepOutSteps_StepOutStepId",
                        column: x => x.StepOutStepId,
                        principalTable: "StepOutSteps",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ActualEntries_StepOutSubSteps_StepOutSubStepId",
                        column: x => x.StepOutSubStepId,
                        principalTable: "StepOutSubSteps",
                        principalColumn: "SubStepId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(name: "IX_ActualEntries_WorkOrderId", table: "ActualEntries", column: "WorkOrderId");
            migrationBuilder.CreateIndex(name: "IX_ActualEntries_FcoDocumentId", table: "ActualEntries", column: "FcoDocumentId");
            migrationBuilder.CreateIndex(name: "IX_ActualEntries_PlanTaskId", table: "ActualEntries", column: "PlanTaskId");
            migrationBuilder.CreateIndex(name: "IX_ActualEntries_StepOutStepId", table: "ActualEntries", column: "StepOutStepId");
            migrationBuilder.CreateIndex(name: "IX_ActualEntries_StepOutSubStepId", table: "ActualEntries", column: "StepOutSubStepId");

            // ── EstimateTaskLinks ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "EstimateTaskLinks",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    EstimateId = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Primary"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateTaskLinks", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_EstimateTaskLinks_PlanTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstimateTaskLinks_TaskId_EstimateId",
                table: "EstimateTaskLinks",
                columns: new[] { "TaskId", "EstimateId" },
                unique: true);

            // ── FcoTaskLinks ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "FcoTaskLinks",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    FcoDocumentId = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "ScopeAddition"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcoTaskLinks", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_FcoTaskLinks_PlanTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FcoTaskLinks_FcoDocuments_FcoDocumentId",
                        column: x => x.FcoDocumentId,
                        principalTable: "FcoDocuments",
                        principalColumn: "FcoDocumentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcoTaskLinks_TaskId_FcoDocumentId",
                table: "FcoTaskLinks",
                columns: new[] { "TaskId", "FcoDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcoTaskLinks_FcoDocumentId",
                table: "FcoTaskLinks",
                column: "FcoDocumentId");

            // ── TimelineBaselines ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TimelineBaselines",
                columns: table => new
                {
                    BaselineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Project"),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationDays = table.Column<int>(type: "int", nullable: true),
                    BaselineReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaselineLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineBaselines", x => x.BaselineId);
                    table.ForeignKey(
                        name: "FK_TimelineBaselines_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineBaselines_ProjectId",
                table: "TimelineBaselines",
                column: "ProjectId");

            // ── TaskProgressSnapshots ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TaskProgressSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PercentComplete = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ForecastEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskProgressSnapshots", x => x.SnapshotId);
                    table.ForeignKey(
                        name: "FK_TaskProgressSnapshots_PlanTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskProgressSnapshots_TaskId",
                table: "TaskProgressSnapshots",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TaskProgressSnapshots");
            migrationBuilder.DropTable(name: "TimelineBaselines");
            migrationBuilder.DropTable(name: "FcoTaskLinks");
            migrationBuilder.DropTable(name: "EstimateTaskLinks");
            migrationBuilder.DropTable(name: "ActualEntries");
            migrationBuilder.DropTable(name: "FcoLaborLines");
            migrationBuilder.DropTable(name: "StepOutSubSteps");

            migrationBuilder.DropForeignKey(name: "FK_FcoDocuments_WorkOrders_LinkedWorkOrderId", table: "FcoDocuments");
            migrationBuilder.DropIndex(name: "IX_FcoDocuments_LinkedWorkOrderId", table: "FcoDocuments");
            migrationBuilder.DropColumn(name: "LinkedWorkOrderId", table: "FcoDocuments");

            migrationBuilder.DropForeignKey(name: "FK_StepOutSteps_StepOutSteps_ParentStepId", table: "StepOutSteps");
            migrationBuilder.DropIndex(name: "IX_StepOutSteps_ParentStepId", table: "StepOutSteps");
            migrationBuilder.DropColumn(name: "ParentStepId", table: "StepOutSteps");
            migrationBuilder.DropColumn(name: "DurationHours", table: "StepOutSteps");
        }
    }
}
