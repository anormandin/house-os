using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Categorie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EquipementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateDocument = table.Column<DateOnly>(type: "date", nullable: true),
                    Echeance = table.Column<DateOnly>(type: "date", nullable: true),
                    NomFichier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CheminDisque = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TypeMime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Taille = table.Column<long>(type: "bigint", nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Equipements_EquipementId",
                        column: x => x.EquipementId,
                        principalTable: "Equipements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Categorie",
                table: "Documents",
                column: "Categorie");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Echeance",
                table: "Documents",
                column: "Echeance");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EquipementId",
                table: "Documents",
                column: "EquipementId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ZoneId",
                table: "Documents",
                column: "ZoneId");

            // Migration des pièces jointes d'équipements : PDF → Manuel, image → Photo,
            // titre = nom de fichier sans extension. Les fichiers disque ne bougent pas.
            migrationBuilder.Sql("""
                INSERT INTO "Documents" ("Id", "Titre", "Categorie", "EquipementId", "ZoneId",
                    "Notes", "DateDocument", "Echeance", "NomFichier", "CheminDisque",
                    "TypeMime", "Taille", "CreeLe")
                SELECT "Id",
                       COALESCE(NULLIF(regexp_replace("NomFichier", '\.[^.]*$', ''), ''), "NomFichier"),
                       CASE WHEN "TypeMime" = 'application/pdf' THEN 'Manuel'
                            WHEN "TypeMime" LIKE 'image/%' THEN 'Photo'
                            ELSE 'Autre' END,
                       "EquipementId", NULL, NULL, NULL, NULL,
                       "NomFichier", "CheminDisque", "TypeMime", "Taille", "CreeLe"
                FROM "PiecesJointes";
                """);

            migrationBuilder.DropTable(
                name: "PiecesJointes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.CreateTable(
                name: "PiecesJointes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheminDisque = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EquipementId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomFichier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Taille = table.Column<long>(type: "bigint", nullable: false),
                    TypeMime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiecesJointes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PiecesJointes_Equipements_EquipementId",
                        column: x => x.EquipementId,
                        principalTable: "Equipements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PiecesJointes_EquipementId",
                table: "PiecesJointes",
                column: "EquipementId");
        }
    }
}
