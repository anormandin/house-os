using System;
using System.Collections.Generic;
using HouseOs.Api.Domaine.Editorial;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterEditionEtEcheanceFerme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EcheanceFerme",
                table: "Taches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Rang = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Surtitre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Manchette = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Chapeau = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Paragraphes = table.Column<string>(type: "jsonb", nullable: false),
                    Rubriques = table.Column<List<RubriqueEdition>>(type: "jsonb", nullable: false),
                    ClesPubliees = table.Column<string>(type: "jsonb", nullable: false),
                    PlancherRaison = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PlancherTitre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Modele = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    GenereLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReeditionEnAttente = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Editions_Date",
                table: "Editions",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Editions");

            migrationBuilder.DropColumn(
                name: "EcheanceFerme",
                table: "Taches");
        }
    }
}
