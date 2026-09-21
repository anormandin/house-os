using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Features.Editorial;

/// <summary>Le créneau du matin de l'éditorialiste, sur le patron du titre d'humeur.</summary>
public class EditorialisteServiceTests
{
    private static readonly EditorialisteService Service = new(
        null!, new RedacteurFictif(), new SignalDeReedition(),
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
