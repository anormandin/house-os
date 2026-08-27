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
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<CompteARebours> ComptesARebours => Set<CompteARebours>();
    public DbSet<PrevisionHoraire> PrevisionsHoraires => Set<PrevisionHoraire>();
    public DbSet<PrevisionQuotidienne> PrevisionsQuotidiennes => Set<PrevisionQuotidienne>();
    public DbSet<ReleveMeteo> RelevesMeteo => Set<ReleveMeteo>();
    public DbSet<PhraseDuJour> PhrasesDuJour => Set<PhraseDuJour>();
    public DbSet<FluxExterne> FluxExternes => Set<FluxExterne>();
    public DbSet<EvenementExterne> EvenementsExternes => Set<EvenementExterne>();
    public DbSet<CompteBudget> ComptesBudget => Set<CompteBudget>();
    public DbSet<Enveloppe> Enveloppes => Set<Enveloppe>();
    public DbSet<MouvementEnveloppe> MouvementsEnveloppe => Set<MouvementEnveloppe>();
    public DbSet<TransactionBancaire> TransactionsBancaires => Set<TransactionBancaire>();

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
            // Jointure seule en cascade : supprimer une tâche ou un document
            // n'efface jamais l'autre entité, seulement le lien.
            t.HasMany(x => x.Documents).WithMany().UsingEntity("TacheDocuments");
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
            // Index FK conservé explicitement : l'index partiel ci-dessous ne sert pas
            // les recherches générales par tâche (cascade de suppression).
            o.HasIndex(x => x.TacheId);
            // Invariant du moteur : une seule occurrence en attente par tâche — ferme la
            // course entre deux complétions simultanées (syntaxe valide Postgres et Sqlite).
            o.HasIndex(x => x.TacheId, "IX_Occurrences_TacheId_EnAttente")
                .IsUnique()
                .HasFilter("\"Statut\" = 'EnAttente'");
        });

        modelBuilder.Entity<EntreeJournal>(j =>
        {
            j.HasOne(x => x.Utilisateur).WithMany().HasForeignKey(x => x.UtilisateurId);
            // Le journal survit à la suppression d'une tâche : pas de FK vers Tache/Occurrence,
            // les ids restent comme références historiques.
            j.HasIndex(x => x.TacheId);
            // Une seule complétion par occurrence : ferme la course de double
            // complétion d'une ponctuelle (aucune occurrence suivante n'y protège).
            j.HasIndex(x => x.OccurrenceId).IsUnique();
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

        modelBuilder.Entity<CompteBudget>(c =>
        {
            c.Property(x => x.Nom).HasMaxLength(200);
            c.Property(x => x.Institution).HasMaxLength(100);
            c.Property(x => x.SoldeInitial).HasPrecision(12, 2);
            c.HasOne(x => x.TacheVirement).WithMany().HasForeignKey(x => x.TacheVirementId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Enveloppe>(e =>
        {
            e.Property(x => x.Nom).HasMaxLength(200);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MontantCible).HasPrecision(12, 2);
            e.Property(x => x.Echeancier).HasColumnType("jsonb");
            // Le lien survit dans l'autre sens : supprimer la tâche ou l'équipement lié
            // rompt le lien (l'enveloppe reste, sans date dérivée → pas de provision).
            e.HasOne(x => x.Tache).WithMany().HasForeignKey(x => x.TacheId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Equipement).WithMany().HasForeignKey(x => x.EquipementId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MouvementEnveloppe>(m =>
        {
            m.Property(x => x.Montant).HasPrecision(12, 2);
            m.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            m.Property(x => x.Note).HasMaxLength(300);
            // Une enveloppe ne se supprime jamais (fermeture seulement) : le journal
            // de mouvements est protégé au niveau du schéma aussi.
            m.HasOne(x => x.Enveloppe).WithMany().HasForeignKey(x => x.EnveloppeId)
                .OnDelete(DeleteBehavior.Restrict);
            m.HasOne(x => x.TransactionBancaire).WithMany().HasForeignKey(x => x.TransactionBancaireId)
                .OnDelete(DeleteBehavior.SetNull);
            m.HasIndex(x => x.EnveloppeId);
            m.HasIndex(x => x.TransactionBancaireId);
        });

        modelBuilder.Entity<TransactionBancaire>(t =>
        {
            t.Property(x => x.Description).HasMaxLength(300);
            t.Property(x => x.IdExterne).HasMaxLength(100);
            t.Property(x => x.CleDedup).HasMaxLength(100);
            t.Property(x => x.Montant).HasPrecision(12, 2);
            t.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
            t.HasOne(x => x.CompteBudget).WithMany().HasForeignKey(x => x.CompteBudgetId)
                .OnDelete(DeleteBehavior.Cascade);
            // La dédup d'import : réimporter le même fichier est sans effet.
            t.HasIndex(x => new { x.CompteBudgetId, x.CleDedup }).IsUnique();
            t.HasIndex(x => x.Statut);
        });

        modelBuilder.Entity<Document>(d =>
        {
            d.Property(x => x.Titre).HasMaxLength(200);
            d.Property(x => x.Categorie).HasConversion<string>().HasMaxLength(20);
            d.Property(x => x.Dossier).HasMaxLength(100);
            d.Property(x => x.Notes).HasMaxLength(2000);
            d.Property(x => x.NomFichier).HasMaxLength(255);
            d.Property(x => x.CheminDisque).HasMaxLength(255);
            d.Property(x => x.TypeMime).HasMaxLength(100);
            // Un document est une archive de la maison : il survit à l'équipement
            // ou la zone qu'il référence.
            d.HasOne<Equipement>().WithMany().HasForeignKey(x => x.EquipementId)
                .OnDelete(DeleteBehavior.SetNull);
            d.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
            d.HasIndex(x => x.Categorie);
            d.HasIndex(x => x.Echeance);
        });
    }
}
