using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterCourrielEntrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AClasser",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportCourrielId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportsCourriel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CleDepot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Expediteur = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Sujet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecuLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraiteLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Erreur = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NbDocuments = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportsCourriel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AClasser",
                table: "Documents",
                column: "AClasser");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ImportCourrielId",
                table: "Documents",
                column: "ImportCourrielId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportsCourriel_CleDepot",
                table: "ImportsCourriel",
                column: "CleDepot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportsCourriel_MessageId",
                table: "ImportsCourriel",
                column: "MessageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ImportsCourriel_ImportCourrielId",
                table: "Documents",
                column: "ImportCourrielId",
                principalTable: "ImportsCourriel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_ImportsCourriel_ImportCourrielId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "ImportsCourriel");

            migrationBuilder.DropIndex(
                name: "IX_Documents_AClasser",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ImportCourrielId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AClasser",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ImportCourrielId",
                table: "Documents");
        }
    }
}
