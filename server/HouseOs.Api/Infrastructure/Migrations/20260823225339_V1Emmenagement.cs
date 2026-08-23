using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1Emmenagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JetonIcal",
                table: "Utilisateurs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipementId",
                table: "Taches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FenetreDebutJour",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FenetreDebutMois",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FenetreFinJour",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FenetreFinMois",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FixeType",
                table: "Taches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalleJours",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JourAnnuel",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JourDuMois",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JoursSemaineMasque",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MoisAnnuel",
                table: "Taches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strategie",
                table: "Taches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Fixe");

            migrationBuilder.AddColumn<Guid>(
                name: "ZoneId",
                table: "Taches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssigneAId",
                table: "Occurrences",
                type: "uuid",
                nullable: true);

            // Données V0 : l'assigné vivait sur la tâche — on le recopie sur les occurrences.
            migrationBuilder.Sql("""
                UPDATE "Occurrences" o SET "AssigneAId" = t."AssigneAId"
                FROM "Taches" t WHERE t."Id" = o."TacheId";
                """);

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Marque = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Modele = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumeroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateAchat = table.Column<DateOnly>(type: "date", nullable: true),
                    FinGarantie = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Specs = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipements_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PiecesJointes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipementId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomFichier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CheminDisque = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TypeMime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Taille = table.Column<long>(type: "bigint", nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_Taches_EquipementId",
                table: "Taches",
                column: "EquipementId");

            migrationBuilder.CreateIndex(
                name: "IX_Taches_ZoneId",
                table: "Taches",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_AssigneAId",
                table: "Occurrences",
                column: "AssigneAId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipements_ZoneId",
                table: "Equipements",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PiecesJointes_EquipementId",
                table: "PiecesJointes",
                column: "EquipementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Utilisateurs_AssigneAId",
                table: "Occurrences",
                column: "AssigneAId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Taches_Equipements_EquipementId",
                table: "Taches",
                column: "EquipementId",
                principalTable: "Equipements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Taches_Zones_ZoneId",
                table: "Taches",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Utilisateurs_AssigneAId",
                table: "Occurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_Taches_Equipements_EquipementId",
                table: "Taches");

            migrationBuilder.DropForeignKey(
                name: "FK_Taches_Zones_ZoneId",
                table: "Taches");

            migrationBuilder.DropTable(
                name: "PiecesJointes");

            migrationBuilder.DropTable(
                name: "Equipements");

            migrationBuilder.DropTable(
                name: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Taches_EquipementId",
                table: "Taches");

            migrationBuilder.DropIndex(
                name: "IX_Taches_ZoneId",
                table: "Taches");

            migrationBuilder.DropIndex(
                name: "IX_Occurrences_AssigneAId",
                table: "Occurrences");

            migrationBuilder.DropColumn(
                name: "JetonIcal",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "EquipementId",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "FenetreDebutJour",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "FenetreDebutMois",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "FenetreFinJour",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "FenetreFinMois",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "FixeType",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "IntervalleJours",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "JourAnnuel",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "JourDuMois",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "JoursSemaineMasque",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "MoisAnnuel",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "Strategie",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Taches");

            migrationBuilder.DropColumn(
                name: "AssigneAId",
                table: "Occurrences");
        }
    }
}
