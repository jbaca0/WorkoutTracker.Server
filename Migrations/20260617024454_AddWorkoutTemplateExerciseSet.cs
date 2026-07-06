using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTemplateExerciseSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseGoals_WorkoutPlans_WorkoutPlanId",
                table: "ExerciseGoals");

            migrationBuilder.DropColumn(
                name: "TargetReps",
                table: "WorkoutTemplateExercises");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutPlanId",
                table: "ExerciseGoals",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExerciseSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkoutTemplateExerciseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExerciseSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExerciseSets_WorkoutTemplateExercises_WorkoutTemplateExerciseId",
                        column: x => x.WorkoutTemplateExerciseId,
                        principalTable: "WorkoutTemplateExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExerciseSets_WorkoutTemplateExerciseId",
                table: "WorkoutTemplateExerciseSets",
                column: "WorkoutTemplateExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseGoals_WorkoutPlans_WorkoutPlanId",
                table: "ExerciseGoals",
                column: "WorkoutPlanId",
                principalTable: "WorkoutPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseGoals_WorkoutPlans_WorkoutPlanId",
                table: "ExerciseGoals");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExerciseSets");

            migrationBuilder.AddColumn<int>(
                name: "TargetReps",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutPlanId",
                table: "ExerciseGoals",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseGoals_WorkoutPlans_WorkoutPlanId",
                table: "ExerciseGoals",
                column: "WorkoutPlanId",
                principalTable: "WorkoutPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
