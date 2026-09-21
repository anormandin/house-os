using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterNormalesClimatiques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoursDeClimat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Coordonnees = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TemperatureMinC = table.Column<double>(type: "double precision", nullable: false),
                    TemperatureMaxC = table.Column<double>(type: "double precision", nullable: false),
                    PrecipitationMm = table.Column<double>(type: "double precision", nullable: false),
                    NeigeCm = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoursDeClimat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalesClimatiques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Coordonnees = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalculeesLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DebutArchive = table.Column<DateOnly>(type: "date", nullable: false),
                    FinArchive = table.Column<DateOnly>(type: "date", nullable: false),
                    SaisonsCompletes = table.Column<int>(type: "integer", nullable: false),
                    PremierGelMois = table.Column<int>(type: "integer", nullable: true),
                    PremierGelJour = table.Column<int>(type: "integer", nullable: true),
                    PremierGelEcartJours = table.Column<int>(type: "integer", nullable: true),
                    PremiereNeigeMois = table.Column<int>(type: "integer", nullable: true),
                    PremiereNeigeJour = table.Column<int>(type: "integer", nullable: true),
                    PremiereNeigeEcartJours = table.Column<int>(type: "integer", nullable: true),
                    DerniereDouceurMois = table.Column<int>(type: "integer", nullable: true),
                    DerniereDouceurJour = table.Column<int>(type: "integer", nullable: true),
                    DerniereDouceurEcartJours = table.Column<int>(type: "integer", nullable: true),
                    MoisLePlusSec = table.Column<int>(type: "integer", nullable: true),
                    MoisLePlusSecMm = table.Column<double>(type: "double precision", nullable: true),
                    MoisLePlusPluvieux = table.Column<int>(type: "integer", nullable: true),
                    MoisLePlusPluvieuxMm = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalesClimatiques", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JoursDeClimat_Coordonnees_Date",
                table: "JoursDeClimat",
                columns: new[] { "Coordonnees", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalesClimatiques_Coordonnees",
                table: "NormalesClimatiques",
                column: "Coordonnees",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoursDeClimat");

            migrationBuilder.DropTable(
                name: "NormalesClimatiques");
        }
    }
}
