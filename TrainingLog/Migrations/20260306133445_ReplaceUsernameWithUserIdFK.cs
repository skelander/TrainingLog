using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingLog.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUsernameWithUserIdFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing sessions so the FK default (0) causes no violation
            migrationBuilder.Sql("DELETE FROM FieldValues;");
            migrationBuilder.Sql("DELETE FROM WorkoutSessions;");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "WorkoutSessions");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "WorkoutSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_Users_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_Users_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkoutSessions");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "WorkoutSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
