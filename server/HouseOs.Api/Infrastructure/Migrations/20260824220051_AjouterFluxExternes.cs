using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterFluxExternes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FluxExternes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    DernierRafraichissementLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DerniereErreur = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FluxExternes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvenementsExternes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FluxExterneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Uid = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Heure = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementsExternes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvenementsExternes_FluxExternes_FluxExterneId",
                        column: x => x.FluxExterneId,
                        principalTable: "FluxExternes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementsExternes_Date",
                table: "EvenementsExternes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementsExternes_FluxExterneId",
                table: "EvenementsExternes",
                column: "FluxExterneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvenementsExternes");

            migrationBuilder.DropTable(
                name: "FluxExternes");
        }
    }
}
