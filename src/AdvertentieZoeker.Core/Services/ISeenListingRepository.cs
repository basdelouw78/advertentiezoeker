namespace AdvertentieZoeker.Core.Services;

/// <summary>Onthoudt welke advertentie-ID's al eerder gezien zijn per zoekopdracht.</summary>
public interface ISeenListingRepository
{
    Task<HashSet<string>> GetSeenIdsAsync(Guid savedSearchId, CancellationToken cancellationToken = default);

    Task AddSeenIdsAsync(Guid savedSearchId, IEnumerable<string> ids, CancellationToken cancellationToken = default);

    /// <summary>Geeft aan of dit de allereerste keer is dat deze zoekopdracht wordt uitgevoerd.</summary>
    Task<bool> IsFirstRunAsync(Guid savedSearchId, CancellationToken cancellationToken = default);
}
