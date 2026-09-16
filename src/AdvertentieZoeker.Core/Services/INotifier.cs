using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

/// <summary>Meldt de gebruiker over nieuw gevonden advertenties voor één zoekopdracht.</summary>
public interface INotifier
{
    /// <param name="isFirstRun">
    /// True als dit de allereerste controle voor deze zoekopdracht is: <paramref name="listings"/>
    /// bevat dan alle huidige advertenties (niet per se "nieuw"), puur ter bevestiging dat de
    /// zoekopdracht werkt. Een notifier die de gebruiker actief stoort (pop-up, e-mail) hoort
    /// dit over te slaan; een notifier die alleen logt (voor het "Gevonden"-scherm) juist niet.
    /// </param>
    Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> listings, bool isFirstRun, CancellationToken cancellationToken = default);
}
