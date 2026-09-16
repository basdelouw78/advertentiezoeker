using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
