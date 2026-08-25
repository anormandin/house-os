using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UneSeuleOccurrenceEnAttenteParTache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_TacheId_EnAttente",
                table: "Occurrences",
                column: "TacheId",
                unique: true,
                filter: "\"Statut\" = 'EnAttente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Occurrences_TacheId_EnAttente",
                table: "Occurrences");
        }
    }
}
