using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingLog.Migrations
{
    /// <inheritdoc />
    public partial class ImproveSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTypes_Name",
                table: "WorkoutTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutTypes_Name",
                table: "WorkoutTypes");
        }
    }
}
