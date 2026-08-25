using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UneCompletionParOccurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Journal_OccurrenceId",
                table: "Journal",
                column: "OccurrenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Journal_OccurrenceId",
                table: "Journal");
        }
    }
}
