using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterAffichage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppareilsAffichage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdresseMac = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Identifiant = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Cle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Modele = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Largeur = table.Column<int>(type: "integer", nullable: true),
                    Hauteur = table.Column<int>(type: "integer", nullable: true),
                    VersionFirmware = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TensionPile = table.Column<double>(type: "double precision", nullable: true),
                    Rssi = table.Column<int>(type: "integer", nullable: true),
                    EnroleLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DernierContact = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DernierFichier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppareilsAffichage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppareilsAffichage_AdresseMac",
                table: "AppareilsAffichage",
                column: "AdresseMac",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppareilsAffichage_Identifiant",
                table: "AppareilsAffichage",
                column: "Identifiant",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppareilsAffichage");
        }
    }
}
