using HouseOs.Api.Domaine;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Infrastructure;

public class HouseOsDbContext(DbContextOptions<HouseOsDbContext> options) : DbContext(options)
{
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Tache> Taches => Set<Tache>();
    public DbSet<Occurrence> Occurrences => Set<Occurrence>();
    public DbSet<EntreeJournal> Journal => Set<EntreeJournal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utilisateur>(u =>
        {
            u.Property(x => x.NomUtilisateur).HasMaxLength(50);
            u.Property(x => x.NomAffichage).HasMaxLength(100);
            u.HasIndex(x => x.NomUtilisateur).IsUnique();
        });

        modelBuilder.Entity<Tache>(t =>
        {
            t.Property(x => x.Titre).HasMaxLength(200);
            t.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
            t.HasMany(x => x.Occurrences).WithOne(o => o.Tache!).HasForeignKey(o => o.TacheId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.AssigneA).WithMany().HasForeignKey(x => x.AssigneAId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Occurrence>(o =>
        {
            o.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
            o.HasOne(x => x.CompleteePar).WithMany().HasForeignKey(x => x.CompleteeParId);
            o.HasIndex(x => new { x.Statut, x.Echeance });
        });

        modelBuilder.Entity<EntreeJournal>(j =>
        {
            j.HasOne(x => x.Utilisateur).WithMany().HasForeignKey(x => x.UtilisateurId);
            // Le journal survit à la suppression d'une tâche : pas de FK vers Tache/Occurrence,
            // les ids restent comme références historiques.
            j.HasIndex(x => x.TacheId);
        });
    }
}
