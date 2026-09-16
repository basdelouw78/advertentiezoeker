using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

public interface ISavedSearchRepository
{
    Task<List<SavedSearch>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAllAsync(List<SavedSearch> searches, CancellationToken cancellationToken = default);
}
