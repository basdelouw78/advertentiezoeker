using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Services;

/// <summary>
/// Draait de controle-cyclus: elke N minuten (instelbaar, standaard 30) worden alle
/// actieve zoekopdrachten opgevraagd en gemeld via <see cref="MonitorService"/>.
/// Op Android wordt dit aangeroepen vanuit <see cref="Platforms.Android.PollingForegroundService"/>
/// zodat het ook doorloopt als de app op de achtergrond staat; op Windows start
/// <c>App.xaml.cs</c> dit rechtstreeks als achtergrondtaak zolang de app open is.
/// </summary>
public sealed class PollingService
{
    private readonly MonitorService _monitorService;
    private readonly ISavedSearchRepository _savedSearchRepository;
    private readonly ISettingsRepository _settingsRepository;

    public PollingService(MonitorService monitorService, ISavedSearchRepository savedSearchRepository, ISettingsRepository settingsRepository)
    {
        _monitorService = monitorService;
        _savedSearchRepository = savedSearchRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var searches = await _savedSearchRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (searches.Count == 0)
        {
            return;
        }

        await _monitorService.CheckAllAsync(searches, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Eén mislukte controle (bijv. geen internet) mag de hele lus niet stoppen.
                System.Diagnostics.Debug.WriteLine($"Advertentiezoeker: controle mislukt: {ex}");
            }

            var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
            var interval = TimeSpan.FromMinutes(Math.Max(5, settings.PollIntervalMinutes));

            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
