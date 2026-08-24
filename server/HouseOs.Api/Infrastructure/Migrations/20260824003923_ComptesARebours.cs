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

            // Amorçage une-seule-fois : reprend les deux comptes codés en dur de
            // l'intérim V0. Une suppression par l'utilisateur ne revient jamais.
            migrationBuilder.InsertData(
                table: "ComptesARebours",
                columns: new[] { "Id", "Titre", "DateCible", "Icone" },
                values: new object[,]
                {
                    { new Guid("5d1cbd75-1a5f-4a09-9de6-0e1c76ac00e1"), "Déménagement", new DateOnly(2026, 10, 6), "Camion" },
                    { new Guid("9b6a7c1e-43d2-4f8a-8f9f-2b64d1f300e2"), "Noël", new DateOnly(2026, 12, 25), "Sapin" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComptesARebours");
        }
    }
}
