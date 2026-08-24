using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Infrastructure;

public class HouseOsDbContext(DbContextOptions<HouseOsDbContext> options) : DbContext(options)
{
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Tache> Taches => Set<Tache>();
    public DbSet<Occurrence> Occurrences => Set<Occurrence>();
    public DbSet<EntreeJournal> Journal => Set<EntreeJournal>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Equipement> Equipements => Set<Equipement>();
    public DbSet<PieceJointe> PiecesJointes => Set<PieceJointe>();
    public DbSet<CompteARebours> ComptesARebours => Set<CompteARebours>();
    public DbSet<PrevisionHoraire> PrevisionsHoraires => Set<PrevisionHoraire>();
    public DbSet<PrevisionQuotidienne> PrevisionsQuotidiennes => Set<PrevisionQuotidienne>();
    public DbSet<ReleveMeteo> RelevesMeteo => Set<ReleveMeteo>();
    public DbSet<PhraseDuJour> PhrasesDuJour => Set<PhraseDuJour>();
    public DbSet<FluxExterne> FluxExternes => Set<FluxExterne>();
    public DbSet<EvenementExterne> EvenementsExternes => Set<EvenementExterne>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utilisateur>(u =>
        {
            u.Property(x => x.NomUtilisateur).HasMaxLength(50);
            u.Property(x => x.NomAffichage).HasMaxLength(100);
            u.Property(x => x.JetonIcal).HasMaxLength(64);
            u.HasIndex(x => x.NomUtilisateur).IsUnique();
        });

        modelBuilder.Entity<Tache>(t =>
        {
            t.Property(x => x.Titre).HasMaxLength(200);
            t.Property(x => x.Strategie).HasConversion<string>().HasMaxLength(20);
            t.HasMany(x => x.Occurrences).WithOne(o => o.Tache!).HasForeignKey(o => o.TacheId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.AssigneA).WithMany().HasForeignKey(x => x.AssigneAId)
                .OnDelete(DeleteBehavior.SetNull);
            t.HasOne(x => x.Zone).WithMany().HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
            t.HasOne(x => x.Equipement).WithMany().HasForeignKey(x => x.EquipementId)
                .OnDelete(DeleteBehavior.SetNull);
            // La spec de récurrence vit dans les colonnes de la table Taches
            // (Mode/Rollover gardent leurs colonnes V0).
            t.OwnsOne(x => x.Recurrence, r =>
            {
                r.Property(p => p.Mode).HasConversion<string>().HasMaxLength(20).HasColumnName("Mode");
                r.Property(p => p.Rollover).HasColumnName("Rollover");
                r.Property(p => p.FixeType).HasConversion<string>().HasMaxLength(20).HasColumnName("FixeType");
                r.Property(p => p.JoursSemaineMasque).HasColumnName("JoursSemaineMasque");
                r.Property(p => p.JourDuMois).HasColumnName("JourDuMois");
                r.Property(p => p.MoisAnnuel).HasColumnName("MoisAnnuel");
                r.Property(p => p.JourAnnuel).HasColumnName("JourAnnuel");
                r.Property(p => p.IntervalleJours).HasColumnName("IntervalleJours");
                r.Property(p => p.FenetreDebutMois).HasColumnName("FenetreDebutMois");
                r.Property(p => p.FenetreDebutJour).HasColumnName("FenetreDebutJour");
                r.Property(p => p.FenetreFinMois).HasColumnName("FenetreFinMois");
                r.Property(p => p.FenetreFinJour).HasColumnName("FenetreFinJour");
            });
            t.Navigation(x => x.Recurrence).IsRequired();
        });

        modelBuilder.Entity<Occurrence>(o =>
        {
            o.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
            o.HasOne(x => x.CompleteePar).WithMany().HasForeignKey(x => x.CompleteeParId);
            o.HasOne(x => x.AssigneA).WithMany().HasForeignKey(x => x.AssigneAId)
                .OnDelete(DeleteBehavior.SetNull);
            o.HasIndex(x => new { x.Statut, x.Echeance });
        });

        modelBuilder.Entity<EntreeJournal>(j =>
        {
            j.HasOne(x => x.Utilisateur).WithMany().HasForeignKey(x => x.UtilisateurId);
            // Le journal survit à la suppression d'une tâche : pas de FK vers Tache/Occurrence,
            // les ids restent comme références historiques.
            j.HasIndex(x => x.TacheId);
        });

        modelBuilder.Entity<Zone>(z =>
        {
            z.Property(x => x.Nom).HasMaxLength(100);
            z.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Equipement>(e =>
        {
            e.Property(x => x.Nom).HasMaxLength(200);
            e.Property(x => x.Marque).HasMaxLength(100);
            e.Property(x => x.Modele).HasMaxLength(100);
            e.Property(x => x.NumeroSerie).HasMaxLength(100);
            e.Property(x => x.Specs).HasColumnType("jsonb");
            e.HasOne(x => x.Zone).WithMany().HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.PiecesJointes).WithOne().HasForeignKey(p => p.EquipementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompteARebours>(c =>
        {
            c.Property(x => x.Titre).HasMaxLength(200);
            c.Property(x => x.Icone).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<PrevisionHoraire>(p =>
        {
            // Heure locale de la maison, sans fuseau (Kind=Unspecified) : timestamptz
            // exigerait des DateTime UTC côté Npgsql.
            p.Property(x => x.Heure).HasColumnType("timestamp without time zone");
            p.HasIndex(x => x.Heure).IsUnique();
        });

        modelBuilder.Entity<PrevisionQuotidienne>(p =>
        {
            p.HasIndex(x => x.Date).IsUnique();
        });

        modelBuilder.Entity<ReleveMeteo>(r =>
        {
            r.Property(x => x.Payload).HasColumnType("jsonb");
        });

        modelBuilder.Entity<FluxExterne>(f =>
        {
            f.Property(x => x.Nom).HasMaxLength(100);
            f.Property(x => x.Url).HasMaxLength(500);
            f.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            f.Property(x => x.DerniereErreur).HasMaxLength(300);
            f.HasMany(x => x.Evenements).WithOne().HasForeignKey(e => e.FluxExterneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvenementExterne>(e =>
        {
            e.Property(x => x.Uid).HasMaxLength(300);
            e.Property(x => x.Titre).HasMaxLength(200);
            e.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<PhraseDuJour>(p =>
        {
            p.Property(x => x.Titre).HasMaxLength(100);
            p.Property(x => x.SousTitre).HasMaxLength(300);
            p.Property(x => x.Moment).HasConversion<string>().HasMaxLength(10);
            p.Property(x => x.Source).HasConversion<string>().HasMaxLength(10);
            p.HasIndex(x => new { x.Date, x.Moment }).IsUnique();
        });

        modelBuilder.Entity<PieceJointe>(p =>
        {
            p.Property(x => x.NomFichier).HasMaxLength(255);
            p.Property(x => x.CheminDisque).HasMaxLength(255);
            p.Property(x => x.TypeMime).HasMaxLength(100);
        });
    }
}
