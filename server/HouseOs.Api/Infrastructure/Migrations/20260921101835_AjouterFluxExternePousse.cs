using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterFluxExternePousse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "FluxExternes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            // « Ics » et non la chaîne vide générée par défaut : les flux déjà en base
            // sont tous des abonnements téléchargés, et une valeur vide ne se relit pas
            // en SourceFluxExterne.
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "FluxExternes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "FluxExternes");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "FluxExternes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
