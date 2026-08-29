using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace HouseOs.Api.Infrastructure.Journalisation;

/// <summary>
/// Colle le <c>TraceId</c> de l'activité courante sur chaque évènement. C'est le fil
/// qui relie, dans Seq, la ligne de requête HTTP, les durées des phases internes et la
/// piste du navigateur — sans lui, chaque log est un fait isolé.
///
/// ASP.NET Core ouvre déjà une <see cref="Activity"/> par requête : on la lit au lieu
/// de fabriquer un identifiant maison (une dépendance de moins que Serilog.Enrichers.Span).
/// </summary>
public sealed class EnrichisseurTrace : ILogEventEnricher
{
    public const string ProprieteTrace = "TraceId";
    public const string ProprieteSpan = "SpanId";

    public void Enrich(LogEvent evenement, ILogEventPropertyFactory fabrique)
    {
        var activite = Activity.Current;
        if (activite is null)
        {
            return;
        }

        evenement.AddPropertyIfAbsent(
            fabrique.CreateProperty(ProprieteTrace, activite.TraceId.ToString()));
        evenement.AddPropertyIfAbsent(
            fabrique.CreateProperty(ProprieteSpan, activite.SpanId.ToString()));
    }
}
