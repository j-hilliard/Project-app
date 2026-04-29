using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stronghold.EnterpriseEstimating.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningLifecycleModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── CommercialAuthorizations ───────────────────────────────────
            migrationBuilder.CreateTable(
                name: "CommercialAuthorizations",
                columns: table => new
                {
                    CommercialAuthorizationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EstimateId = table.Column<int>(type: "int", nullable: false),
                    AuthorizationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuthorizationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuthorizedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AuthorizedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialAuthorizations", x => x.CommercialAuthorizationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommercialAuthorizations_CompanyCode_AuthorizationNumber",
                table: "CommercialAuthorizations",
                columns: new[] { "CompanyCode", "AuthorizationNumber" });

            // ── Projects ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstimateId = table.Column<int>(type: "int", nullable: false),
                    CommercialAuthorizationId = table.Column<int>(type: "int", nullable: true),
                    Client = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Site = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JobLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ForecastEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Initiating"),
                    AtRiskThresholdDays = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    OwnerUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LessonsLearnedNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_CommercialAuthorizations_CommercialAuthorizationId",
                        column: x => x.CommercialAuthorizationId,
                        principalTable: "CommercialAuthorizations",
                        principalColumn: "CommercialAuthorizationId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyCode_ProjectNumber",
                table: "Projects",
                columns: new[] { "CompanyCode", "ProjectNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CommercialAuthorizationId",
                table: "Projects",
                column: "CommercialAuthorizationId");

            // ── WorkOrders ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    WorkOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    EstimateId = table.Column<int>(type: "int", nullable: false),
                    CommercialAuthorizationId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ForecastEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    ReleasedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.WorkOrderId);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_CommercialAuthorizations_CommercialAuthorizationId",
                        column: x => x.CommercialAuthorizationId,
                        principalTable: "CommercialAuthorizations",
                        principalColumn: "CommercialAuthorizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CompanyCode_WorkOrderNumber",
                table: "WorkOrders",
                columns: new[] { "CompanyCode", "WorkOrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProjectId",
                table: "WorkOrders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CommercialAuthorizationId",
                table: "WorkOrders",
                column: "CommercialAuthorizationId");

            // ── ProjectPhases ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ProjectPhases",
                columns: table => new
                {
                    PhaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ForecastEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Planning"),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPhases", x => x.PhaseId);
                    table.ForeignKey(
                        name: "FK_ProjectPhases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhases_ProjectId",
                table: "ProjectPhases",
                column: "ProjectId");

            // ── PlanTasks ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "PlanTasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhaseId = table.Column<int>(type: "int", nullable: false),
                    ParentTaskId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Task"),
                    LinkedEstimateId = table.Column<int>(type: "int", nullable: true),
                    LinkedFcoDocumentId = table.Column<int>(type: "int", nullable: true),
                    LinkedStepOutPlanId = table.Column<int>(type: "int", nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    BaselineStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineDuration = table.Column<int>(type: "int", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ForecastEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PercentComplete = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    CraftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OwnerUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsMilestone = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_PlanTasks_ProjectPhases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "ProjectPhases",
                        principalColumn: "PhaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanTasks_PlanTasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_PhaseId",
                table: "PlanTasks",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTasks_ParentTaskId",
                table: "PlanTasks",
                column: "ParentTaskId");

            // ── TaskDependencies ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    DependencyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuccessorTaskId = table.Column<int>(type: "int", nullable: false),
                    PredecessorTaskId = table.Column<int>(type: "int", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "FinishToStart"),
                    LagDays = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.DependencyId);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_PlanTasks_SuccessorTaskId",
                        column: x => x.SuccessorTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_PlanTasks_PredecessorTaskId",
                        column: x => x.PredecessorTaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_SuccessorTaskId",
                table: "TaskDependencies",
                column: "SuccessorTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_PredecessorTaskId",
                table: "TaskDependencies",
                column: "PredecessorTaskId");

            // ── Milestones ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    PhaseId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaselineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    IsDeadline = table.Column<bool>(type: "bit", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.MilestoneId);
                    table.ForeignKey(
                        name: "FK_Milestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Milestones_ProjectPhases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "ProjectPhases",
                        principalColumn: "PhaseId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Milestones_PlanTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PlanTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_PhaseId",
                table: "Milestones",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_TaskId",
                table: "Milestones",
                column: "TaskId");

            // ── Extend StepOutPlans with WorkOrderId ───────────────────────
            migrationBuilder.AddColumn<int>(
                name: "WorkOrderId",
                table: "StepOutPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepOutPlans_WorkOrderId",
                table: "StepOutPlans",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_StepOutPlans_WorkOrders_WorkOrderId",
                table: "StepOutPlans",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.SetNull);

            // ── Extend WorkPackages with WorkOrderId ───────────────────────
            migrationBuilder.AddColumn<int>(
                name: "WorkOrderId",
                table: "WorkPackages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkPackages_WorkOrderId",
                table: "WorkPackages",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPackages_WorkOrders_WorkOrderId",
                table: "WorkPackages",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove WorkPackages FK/column
            migrationBuilder.DropForeignKey(name: "FK_WorkPackages_WorkOrders_WorkOrderId", table: "WorkPackages");
            migrationBuilder.DropIndex(name: "IX_WorkPackages_WorkOrderId", table: "WorkPackages");
            migrationBuilder.DropColumn(name: "WorkOrderId", table: "WorkPackages");

            // Remove StepOutPlans FK/column
            migrationBuilder.DropForeignKey(name: "FK_StepOutPlans_WorkOrders_WorkOrderId", table: "StepOutPlans");
            migrationBuilder.DropIndex(name: "IX_StepOutPlans_WorkOrderId", table: "StepOutPlans");
            migrationBuilder.DropColumn(name: "WorkOrderId", table: "StepOutPlans");

            // Drop in reverse dependency order
            migrationBuilder.DropTable(name: "Milestones");
            migrationBuilder.DropTable(name: "TaskDependencies");
            migrationBuilder.DropTable(name: "PlanTasks");
            migrationBuilder.DropTable(name: "ProjectPhases");
            migrationBuilder.DropTable(name: "WorkOrders");
            migrationBuilder.DropTable(name: "Projects");
            migrationBuilder.DropTable(name: "CommercialAuthorizations");
        }
    }
}
