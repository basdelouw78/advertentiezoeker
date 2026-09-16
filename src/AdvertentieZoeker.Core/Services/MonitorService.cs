using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

/// <summary>
/// Voert alle actieve zoekopdrachten één keer uit, bepaalt welke advertenties nieuw zijn
/// sinds de vorige keer, en stuurt die door naar alle geregistreerde notifiers.
/// Bij de allereerste keer dat een zoekopdracht draait, wordt de huidige stand van zaken
/// alleen "gebaseline" (opgeslagen als gezien) zonder meldingen te sturen — anders zou je
/// bij het toevoegen van een zoekopdracht meteen een lawine aan meldingen krijgen voor
/// advertenties die er al lang stonden.
/// </summary>
public sealed class MonitorService
{
    private readonly IMarktplaatsClient _client;
    private readonly ISeenListingRepository _seenListingRepository;
    private readonly IReadOnlyList<INotifier> _notifiers;

    public MonitorService(IMarktplaatsClient client, ISeenListingRepository seenListingRepository, IEnumerable<INotifier> notifiers)
    {
        _client = client;
        _seenListingRepository = seenListingRepository;
        _notifiers = notifiers.ToList();
    }

    public async Task<IReadOnlyList<Listing>> CheckAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        var listings = await _client.SearchAsync(search, cancellationToken).ConfigureAwait(false);
        var isFirstRun = await _seenListingRepository.IsFirstRunAsync(search.Id, cancellationToken).ConfigureAwait(false);

        if (isFirstRun)
        {
            await _seenListingRepository.AddSeenIdsAsync(search.Id, listings.Select(l => l.Id), cancellationToken).ConfigureAwait(false);
            return Array.Empty<Listing>();
        }

        var seenIds = await _seenListingRepository.GetSeenIdsAsync(search.Id, cancellationToken).ConfigureAwait(false);
        var newListings = listings.Where(l => !seenIds.Contains(l.Id)).ToList();

        if (newListings.Count > 0)
        {
            await _seenListingRepository.AddSeenIdsAsync(search.Id, newListings.Select(l => l.Id), cancellationToken).ConfigureAwait(false);

            foreach (var notifier in _notifiers)
            {
                await notifier.NotifyNewListingsAsync(search, newListings, cancellationToken).ConfigureAwait(false);
            }
        }

        return newListings;
    }

    public async Task<IReadOnlyDictionary<Guid, SearchCheckOutcome>> CheckAllAsync(
        IEnumerable<SavedSearch> searches, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, SearchCheckOutcome>();
        foreach (var search in searches.Where(s => s.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var newListings = await CheckAsync(search, cancellationToken).ConfigureAwait(false);
                result[search.Id] = new SearchCheckOutcome(newListings, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Eén zoekopdracht die faalt (bijv. een tijdelijke netwerkfout, of Marktplaats
                // die voor deze specifieke combinatie van parameters iets onverwachts teruggeeft)
                // mag de andere zoekopdrachten in deze ronde niet blokkeren.
                result[search.Id] = new SearchCheckOutcome(Array.Empty<Listing>(), ex);
            }
        }

        return result;
    }
}

/// <summary>Resultaat van één zoekopdracht binnen een controle-ronde.</summary>
public sealed record SearchCheckOutcome(IReadOnlyList<Listing> NewListings, Exception? Error);
