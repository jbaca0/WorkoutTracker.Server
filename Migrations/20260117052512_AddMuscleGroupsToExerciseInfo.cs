using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMuscleGroupsToExerciseInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryMuscleGroup",
                table: "ExerciseInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryMuscleGroupsJson",
                table: "ExerciseInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryMuscleGroup",
                table: "ExerciseInfos");

            migrationBuilder.DropColumn(
                name: "SecondaryMuscleGroupsJson",
                table: "ExerciseInfos");
        }
    }
}
