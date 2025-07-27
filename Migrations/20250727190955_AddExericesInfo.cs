using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExericesInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_ExerciseInfo_ExerciseInfoId",
                table: "Exercise");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExerciseInfo",
                table: "ExerciseInfo");

            migrationBuilder.RenameTable(
                name: "ExerciseInfo",
                newName: "ExerciseInfos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExerciseInfos",
                table: "ExerciseInfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_ExerciseInfos_ExerciseInfoId",
                table: "Exercise",
                column: "ExerciseInfoId",
                principalTable: "ExerciseInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercise_ExerciseInfos_ExerciseInfoId",
                table: "Exercise");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExerciseInfos",
                table: "ExerciseInfos");

            migrationBuilder.RenameTable(
                name: "ExerciseInfos",
                newName: "ExerciseInfo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExerciseInfo",
                table: "ExerciseInfo",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercise_ExerciseInfo_ExerciseInfoId",
                table: "Exercise",
                column: "ExerciseInfoId",
                principalTable: "ExerciseInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
