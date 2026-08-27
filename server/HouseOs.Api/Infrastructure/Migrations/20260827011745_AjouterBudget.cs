using System;
using System.Collections.Generic;
using HouseOs.Api.Domaine;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseOs.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjouterBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComptesBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Institution = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoldeInitial = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DateAncrage = table.Column<DateOnly>(type: "date", nullable: false),
                    TacheVirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComptesBudget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComptesBudget_Taches_TacheVirementId",
                        column: x => x.TacheVirementId,
                        principalTable: "Taches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Enveloppes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MontantCible = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    DateCible = table.Column<DateOnly>(type: "date", nullable: true),
                    TacheId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Echeancier = table.Column<List<Versement>>(type: "jsonb", nullable: true),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enveloppes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enveloppes_Equipements_EquipementId",
                        column: x => x.EquipementId,
                        principalTable: "Equipements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Enveloppes_Taches_TacheId",
                        column: x => x.TacheId,
                        principalTable: "Taches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TransactionsBancaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompteBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IdExterne = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CleDedup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImporteeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionsBancaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionsBancaires_ComptesBudget_CompteBudgetId",
                        column: x => x.CompteBudgetId,
                        principalTable: "ComptesBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MouvementsEnveloppe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnveloppeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransactionBancaireId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntreeJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouvementsEnveloppe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MouvementsEnveloppe_Enveloppes_EnveloppeId",
                        column: x => x.EnveloppeId,
                        principalTable: "Enveloppes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MouvementsEnveloppe_TransactionsBancaires_TransactionBancai~",
                        column: x => x.TransactionBancaireId,
                        principalTable: "TransactionsBancaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComptesBudget_TacheVirementId",
                table: "ComptesBudget",
                column: "TacheVirementId");

            migrationBuilder.CreateIndex(
                name: "IX_Enveloppes_EquipementId",
                table: "Enveloppes",
                column: "EquipementId");

            migrationBuilder.CreateIndex(
                name: "IX_Enveloppes_TacheId",
                table: "Enveloppes",
                column: "TacheId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsEnveloppe_EnveloppeId",
                table: "MouvementsEnveloppe",
                column: "EnveloppeId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsEnveloppe_TransactionBancaireId",
                table: "MouvementsEnveloppe",
                column: "TransactionBancaireId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsBancaires_CompteBudgetId_CleDedup",
                table: "TransactionsBancaires",
                columns: new[] { "CompteBudgetId", "CleDedup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsBancaires_Statut",
                table: "TransactionsBancaires",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MouvementsEnveloppe");

            migrationBuilder.DropTable(
                name: "Enveloppes");

            migrationBuilder.DropTable(
                name: "TransactionsBancaires");

            migrationBuilder.DropTable(
                name: "ComptesBudget");
        }
    }
}
