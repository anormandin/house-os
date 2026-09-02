using System.Text.Json;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HouseOs.Tests.Features.Taches;

/// <summary>
/// Contexte de test sur Sqlite in-memory : différences avec Postgres — la colonne
/// jsonb Equipement.Specs est re-mappée en TEXT via une conversion JSON, et les
/// DateTimeOffset comparés en requête (journal) sont convertis en binaire, car le
/// fournisseur Sqlite ne sait pas les traduire.
/// </summary>
public class HouseOsDbContextSqlite(DbContextOptions<HouseOsDbContext> options)
    : HouseOsDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Equipement>().Property(e => e.Specs)
            .HasColumnType("TEXT")
            .HasConversion(
                specs => JsonSerializer.Serialize(specs, (JsonSerializerOptions?)null),
                texte => JsonSerializer.Deserialize<Dictionary<string, string>>(
                    texte, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        // Même remède pour l'échéancier jsonb des enveloppes budgétaires.
        modelBuilder.Entity<Enveloppe>().Property(e => e.Echeancier)
            .HasColumnType("TEXT")
            .HasConversion(
                echeancier => JsonSerializer.Serialize(echeancier, (JsonSerializerOptions?)null),
                texte => JsonSerializer.Deserialize<List<Versement>>(texte, (JsonSerializerOptions?)null));
        modelBuilder.Entity<EntreeJournal>().Property(j => j.CompleteeLe)
            .HasConversion(new DateTimeOffsetToBinaryConverter());
        // Le tri des complétées (ListerOccurrencesAsync) ordonne par CompleteeLe :
        // même limite Sqlite, même remède.
        modelBuilder.Entity<Occurrence>().Property(o => o.CompleteeLe)
            .HasConversion(new DateTimeOffsetToBinaryConverter());
        // Les listes de documents (REST et MCP) trient par CreeLe.
        modelBuilder.Entity<Document>().Property(d => d.CreeLe)
            .HasConversion(new DateTimeOffsetToBinaryConverter());
    }
}

/// <summary>Base commune : ouvre la connexion in-memory (vivante tant que le test vit).</summary>
public abstract class TestAvecSqlite : IDisposable
{
    private readonly SqliteConnection _connexion;
    protected HouseOsDbContext Db { get; }

    /// <summary>La connexion partagée — pour les tests qui fabriquent leurs propres
    /// scopes DI (un DbContext par flux) sur la même base in-memory.</summary>
    protected SqliteConnection Connexion => _connexion;

    protected TestAvecSqlite()
    {
        _connexion = new SqliteConnection("DataSource=:memory:");
        _connexion.Open();
        var options = new DbContextOptionsBuilder<HouseOsDbContext>()
            .UseSqlite(_connexion)
            .Options;
        Db = new HouseOsDbContextSqlite(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connexion.Dispose();
        GC.SuppressFinalize(this);
    }
}
