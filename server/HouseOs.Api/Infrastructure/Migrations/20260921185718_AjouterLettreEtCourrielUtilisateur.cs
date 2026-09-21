using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterLettreEtCourrielUtilisateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Courriel",
                table: "Utilisateurs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lettres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Sujet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Paragraphes = table.Column<string>(type: "jsonb", nullable: false),
                    Source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Modele = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Matiere = table.Column<string>(type: "jsonb", nullable: true),
                    ComposeeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnvoyeeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Destinataires = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lettres", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lettres_Date",
                table: "Lettres",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lettres");

            migrationBuilder.DropColumn(
                name: "Courriel",
                table: "Utilisateurs");
        }
    }
}
