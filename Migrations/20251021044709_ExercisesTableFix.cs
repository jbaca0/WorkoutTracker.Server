using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class ExercisesTableFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_ExerciseInfos_ExerciseInfoId",
                table: "Exercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_Workouts_WorkoutId",
                table: "Exercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Exercise_ExerciseId",
                table: "Set");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercise",
                table: "Exercise");

            migrationBuilder.RenameTable(
                name: "Exercise",
                newName: "Exercises");

            migrationBuilder.RenameIndex(
                name: "IX_Exercise_WorkoutId",
                table: "Exercises",
                newName: "IX_Exercises_WorkoutId");

            migrationBuilder.RenameIndex(
                name: "IX_Exercise_ExerciseInfoId",
                table: "Exercises",
                newName: "IX_Exercises_ExerciseInfoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_ExerciseInfos_ExerciseInfoId",
                table: "Exercises",
                column: "ExerciseInfoId",
                principalTable: "ExerciseInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Workouts_WorkoutId",
                table: "Exercises",
                column: "WorkoutId",
                principalTable: "Workouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Exercises_ExerciseId",
                table: "Set",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_ExerciseInfos_ExerciseInfoId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Workouts_WorkoutId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Exercises_ExerciseId",
                table: "Set");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises");

            migrationBuilder.RenameTable(
                name: "Exercises",
                newName: "Exercise");

            migrationBuilder.RenameIndex(
                name: "IX_Exercises_WorkoutId",
                table: "Exercise",
                newName: "IX_Exercise_WorkoutId");

            migrationBuilder.RenameIndex(
                name: "IX_Exercises_ExerciseInfoId",
                table: "Exercise",
                newName: "IX_Exercise_ExerciseInfoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercise",
                table: "Exercise",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_ExerciseInfos_ExerciseInfoId",
                table: "Exercise",
                column: "ExerciseInfoId",
                principalTable: "ExerciseInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_Workouts_WorkoutId",
                table: "Exercise",
                column: "WorkoutId",
                principalTable: "Workouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Exercise_ExerciseId",
                table: "Set",
                column: "ExerciseId",
                principalTable: "Exercise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
