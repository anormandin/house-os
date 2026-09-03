using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ComptesARebours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComptesARebours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateCible = table.Column<DateOnly>(type: "date", nullable: false),
                    Icone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComptesARebours", x => x.Id);
                });

            // Aucune donnée d'amorçage : les comptes à rebours d'un foyer lui appartiennent.
            // (L'InsertData d'origine, propre au premier foyer, a été retiré le 2026-09-02 ;
            // sans effet sur une base où la migration est déjà appliquée.)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComptesARebours");
        }
    }
}
