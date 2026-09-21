using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Features.Editorial;

/// <summary>Le créneau du matin de l'éditorialiste, sur le patron du titre d'humeur.</summary>
public class EditorialisteServiceTests
{
    private static readonly RedacteurFictif Redacteur = new();
    private static readonly EditorialisteService Service = new(
        null!, Redacteur, new SignalDeReedition(),
        Options.Create(new HumeurOptions()), Options.Create(new HouseOs.Api.Features.Meteo.MeteoOptions()),
        Options.Create(new HouseOs.Api.Features.Affichage.AffichageOptions()), null!,
        NullLogger<EditorialisteService>.Instance);

    private static DateTimeOffset Local(int heure, int minute)
    {
        var mural = new DateTime(2026, 9, 22, heure, minute, 0);
        return new DateTimeOffset(mural, TimeZoneInfo.Local.GetUtcOffset(mural));
    }

    [Theory]
    [InlineData(3, 0, 22)]
    [InlineData(10, 0, 23)]
    [InlineData(23, 30, 23)]
    public void Le_prochain_creneau_est_le_prochain_matin_avec_une_minute_de_marge(int heure, int minute, int jourAttendu)
    {
        var prochain = Service.ProchainCreneau(Local(heure, minute));

        Assert.True(prochain > Local(heure, minute));
        Assert.Equal(new DateOnly(2026, 9, jourAttendu), DateOnly.FromDateTime(prochain.LocalDateTime));
        Assert.Equal(5, prochain.LocalDateTime.Hour);
        Assert.Equal(31, prochain.LocalDateTime.Minute);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(5, 29)]
    [InlineData(23, 59)]
    public void L_edition_visee_est_celle_du_jour_civil_meme_avant_l_heure_du_matin(int heure, int minute)
    {
        // Le rattrapage de trois heures du matin écrit l'édition de la journée qui
        // commence, jamais celle de la veille (revue de l'étape 7, posé à l'étape 8 :
        // le code portait encore « la veille avant l'heure du matin »).
        Assert.Equal(new DateOnly(2026, 9, 22),
            EditorialisteService.DateDEdition(new DateTime(2026, 9, 22, heure, minute, 0)));
    }

    [Fact]
    public void Un_gabarit_ecrit_avec_une_cle_merite_un_second_essai_une_heure_plus_tard()
    {
        var genereLe = Local(5, 31);
        var gabarit = new Edition
        {
            Date = new DateOnly(2026, 9, 22), Manchette = "x", Source = SourceEdition.Gabarit, GenereLe = genereLe,
        };
        var opus = new Edition
        {
            Date = new DateOnly(2026, 9, 22), Manchette = "x", Source = SourceEdition.Llm, GenereLe = genereLe,
        };

        var unPeuApres = genereLe.AddMinutes(5);
        Assert.Equal(genereLe + EditorialisteService.DelaiDeReessai, Service.MomentDuReessai(gabarit, unPeuApres));
        Assert.Null(Service.MomentDuReessai(opus, unPeuApres));
        // Passé la fenêtre (deux fois le délai), un redémarrage ne rappelle pas le modèle
        // pour le gabarit du matin.
        Assert.Null(Service.MomentDuReessai(gabarit, genereLe.AddHours(2)));
        Assert.NotNull(Service.MomentDuReessai(gabarit, genereLe.AddMinutes(119)));

        // Sans clé, le gabarit est le fonctionnement normal : rien à réessayer.
        var sansCle = new EditorialisteService(
            null!, new RedacteurFictif { PeutEcrire = false }, new SignalDeReedition(),
            Options.Create(new HumeurOptions()), Options.Create(new HouseOs.Api.Features.Meteo.MeteoOptions()),
            Options.Create(new HouseOs.Api.Features.Affichage.AffichageOptions()), null!,
            NullLogger<EditorialisteService>.Instance);
        Assert.Null(sansCle.MomentDuReessai(gabarit, unPeuApres));
    }

    [Fact]
    public void Le_prochain_reveil_est_le_second_essai_quand_il_vient_avant_le_creneau()
    {
        var maintenant = Local(5, 32);
        var reessai = Local(6, 31);

        Assert.Equal(reessai, Service.ProchainReveil(maintenant, reessai));
        Assert.Equal(Service.ProchainCreneau(maintenant), Service.ProchainReveil(maintenant, null));
        // Un essai prévu après le créneau du lendemain n'avance rien : le créneau gagne.
        Assert.Equal(Service.ProchainCreneau(maintenant), Service.ProchainReveil(maintenant, Local(5, 31).AddDays(2)));
    }

    [Fact]
    public async Task Le_signal_reveille_sans_attendre_et_ne_s_empile_pas()
    {
        var signal = new SignalDeReedition();
        Assert.False(await signal.AttendreAsync(TimeSpan.Zero, CancellationToken.None));

        signal.Demander();
        signal.Demander();
        Assert.True(await signal.AttendreAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
        Assert.False(await signal.AttendreAsync(TimeSpan.Zero, CancellationToken.None));
    }
}
