using AdvertentieZoeker.App.Services;
#if WINDOWS
using Microsoft.Windows.AppNotifications;
#endif

namespace AdvertentieZoeker.App;

public partial class App : Application
{
    private readonly PollingService _pollingService;
#if WINDOWS
    private CancellationTokenSource? _pollingCts;
#endif

    public App(PollingService pollingService)
    {
        InitializeComponent();
        _pollingService = pollingService;

        // Deze app heeft alleen een licht kleurenschema; forceer dat ook als het toestel
        // zelf op donkere modus staat (anders volgen niet-expliciet gestylede besturings-
        // elementen het systeemthema en wordt bijv. tekst onleesbaar wit-op-wit).
        UserAppTheme = AppTheme.Light;

        MainPage = new AppShell();
    }

    protected override void OnStart()
    {
        base.OnStart();

#if WINDOWS
        AppNotificationManager.Default.Register();

        // Op Windows draait de app als gewoon proces: zolang het open is, laten we
        // hier de 30-minuten-controle lopen. Op Android gebeurt dit via de
        // achtergrond-service (zie Platforms/Android/PollingForegroundService),
        // omdat Android achtergrondtaken in het app-proces zelf hard beperkt.
        _pollingCts = new CancellationTokenSource();
        _ = _pollingService.RunForeverAsync(_pollingCts.Token);
#endif
    }

    protected override void OnSleep()
    {
        base.OnSleep();
#if WINDOWS
        _pollingCts?.Cancel();
#endif
    }

    protected override void OnResume()
    {
        base.OnResume();
#if WINDOWS
        _pollingCts = new CancellationTokenSource();
        _ = _pollingService.RunForeverAsync(_pollingCts.Token);
#endif
    }
}
