using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterMeteo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrevisionsHoraires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Heure = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    PrecipitationMm = table.Column<double>(type: "double precision", nullable: false),
                    ProbabilitePrecipitationPct = table.Column<int>(type: "integer", nullable: false),
                    VentKmh = table.Column<double>(type: "double precision", nullable: false),
                    RafalesKmh = table.Column<double>(type: "double precision", nullable: false),
                    HumiditePct = table.Column<int>(type: "integer", nullable: false),
                    HumiditeSol = table.Column<double>(type: "double precision", nullable: true),
                    CouvertureNuageusePct = table.Column<int>(type: "integer", nullable: false),
                    IndiceUv = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrevisionsHoraires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrevisionsQuotidiennes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TemperatureMinC = table.Column<double>(type: "double precision", nullable: false),
                    TemperatureMaxC = table.Column<double>(type: "double precision", nullable: false),
                    PrecipitationMm = table.Column<double>(type: "double precision", nullable: false),
                    ProbabilitePrecipitationMaxPct = table.Column<int>(type: "integer", nullable: false),
                    VentMaxKmh = table.Column<double>(type: "double precision", nullable: false),
                    RafalesMaxKmh = table.Column<double>(type: "double precision", nullable: false),
                    IndiceUvMax = table.Column<double>(type: "double precision", nullable: false),
                    CodeMeteo = table.Column<int>(type: "integer", nullable: false),
                    Lever = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Coucher = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrevisionsQuotidiennes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelevesMeteo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecupereLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelevesMeteo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrevisionsHoraires_Heure",
                table: "PrevisionsHoraires",
                column: "Heure",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrevisionsQuotidiennes_Date",
                table: "PrevisionsQuotidiennes",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrevisionsHoraires");

            migrationBuilder.DropTable(
                name: "PrevisionsQuotidiennes");

            migrationBuilder.DropTable(
                name: "RelevesMeteo");
        }
    }
}
