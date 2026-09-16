using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

/// <summary>Meldt de gebruiker over nieuw gevonden advertenties voor één zoekopdracht.</summary>
public interface INotifier
{
    Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, CancellationToken cancellationToken = default);
}
