using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTemplatesAndPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPlanDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkoutPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanDays_WorkoutPlans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "WorkoutPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanDays_WorkoutTemplates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseInfoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_ExerciseInfos_ExerciseInfoId",
                        column: x => x.ExerciseInfoId,
                        principalTable: "ExerciseInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_WorkoutTemplates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanDays_WorkoutPlanId",
                table: "WorkoutPlanDays",
                column: "WorkoutPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanDays_WorkoutTemplateId",
                table: "WorkoutPlanDays",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_ExerciseInfoId",
                table: "WorkoutTemplateExercises",
                column: "ExerciseInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_WorkoutTemplateId",
                table: "WorkoutTemplateExercises",
                column: "WorkoutTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutPlanDays");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercises");

            migrationBuilder.DropTable(
                name: "WorkoutPlans");

            migrationBuilder.DropTable(
                name: "WorkoutTemplates");
        }
    }
}
