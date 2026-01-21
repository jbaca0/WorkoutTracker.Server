using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class PrimaryAndSecondaryMuscleGroupNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SecondaryMuscleGroupsJson",
                table: "ExerciseInfos",
                type: "TEXT",
                nullable: true,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryMuscleGroup",
                table: "ExerciseInfos",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 9);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SecondaryMuscleGroupsJson",
                table: "ExerciseInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true,
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryMuscleGroup",
                table: "ExerciseInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true,
                oldDefaultValue: 0);
        }
    }
}
