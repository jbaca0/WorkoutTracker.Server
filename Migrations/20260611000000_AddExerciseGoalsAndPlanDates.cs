using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseGoalsAndPlanDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "WorkoutPlans",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "WorkoutPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetReps",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ExerciseGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseInfoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkoutPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartReps = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    TargetReps = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseGoals_ExerciseInfos_ExerciseInfoId",
                        column: x => x.ExerciseInfoId,
                        principalTable: "ExerciseInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseGoals_WorkoutPlans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "WorkoutPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGoals_ExerciseInfoId",
                table: "ExerciseGoals",
                column: "ExerciseInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGoals_WorkoutPlanId",
                table: "ExerciseGoals",
                column: "WorkoutPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGoals_UserId_ExerciseInfoId",
                table: "ExerciseGoals",
                columns: new[] { "UserId", "ExerciseInfoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseGoals");

            migrationBuilder.DropColumn(
                name: "TargetReps",
                table: "WorkoutTemplateExercises");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "WorkoutPlans");
        }
    }
}
