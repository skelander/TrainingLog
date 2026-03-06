using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingLog.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDeleteBehaviors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                table: "FieldValues");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutTypes_WorkoutTypeId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                table: "FieldValues",
                column: "FieldDefinitionId",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutTypes_WorkoutTypeId",
                table: "WorkoutSessions",
                column: "WorkoutTypeId",
                principalTable: "WorkoutTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                table: "FieldValues");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutTypes_WorkoutTypeId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldValues_FieldDefinitions_FieldDefinitionId",
                table: "FieldValues",
                column: "FieldDefinitionId",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutTypes_WorkoutTypeId",
                table: "WorkoutSessions",
                column: "WorkoutTypeId",
                principalTable: "WorkoutTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
