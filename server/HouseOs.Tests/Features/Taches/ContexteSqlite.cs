using System.Text.Json;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Tests.Features.Taches;

/// <summary>
/// Contexte de test sur Sqlite in-memory : seule différence avec Postgres, la colonne
/// jsonb Equipement.Specs est re-mappée en TEXT via une conversion JSON.
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
    }
}

/// <summary>Base commune : ouvre la connexion in-memory (vivante tant que le test vit).</summary>
public abstract class TestAvecSqlite : IDisposable
{
    private readonly SqliteConnection _connexion;
    protected HouseOsDbContext Db { get; }

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
