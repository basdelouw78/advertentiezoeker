using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

public interface IMarktplaatsClient
{
    Task<IReadOnlyList<Listing>> SearchAsync(SavedSearch search, CancellationToken cancellationToken = default);
}
