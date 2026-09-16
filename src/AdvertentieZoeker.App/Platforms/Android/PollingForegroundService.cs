using Android.App;
using Android.Content;
using Android.OS;
using AdvertentieZoeker.App.Services;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;

namespace AdvertentieZoeker.App;

/// <summary>
/// Achtergrondservice die Android nodig heeft om de app buiten beeld actief te houden.
/// Toont verplicht een permanente, onopvallende melding ("Advertentiezoeker actief") en
/// draait daarbinnen de eigenlijke <see cref="PollingService"/>-lus.
/// </summary>
[Service(Exported = false)]
public class PollingForegroundService : Service
{
    private const string ChannelId = "advertentiezoeker_service";
    private const int ForegroundNotificationId = 1;

    private CancellationTokenSource? _cts;

    public static void Start(Context context)
    {
        var intent = new Intent(context, typeof(PollingForegroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_cts is not null)
        {
            // Service draait al (bijv. de app is opnieuw geopend); niet nog een keer starten.
            return StartCommandResult.Sticky;
        }

        StartForeground(ForegroundNotificationId, BuildNotification());

        _cts = new CancellationTokenSource();
        var pollingService = IPlatformApplication.Current?.Services.GetService<PollingService>();
        if (pollingService is not null)
        {
            _ = pollingService.RunForeverAsync(_cts.Token);
        }

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        _cts = null;
        base.OnDestroy();
    }

    private Notification BuildNotification()
    {
        var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O
            && notificationManager.GetNotificationChannel(ChannelId) is null)
        {
            var channel = new NotificationChannel(ChannelId, "Advertentiezoeker", NotificationImportance.Low)
            {
                Description = "Houdt de zoekopdrachten actief op de achtergrond.",
            };
            notificationManager.CreateNotificationChannel(channel);
        }

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Advertentiezoeker actief")
            .SetContentText("Marktplaats wordt periodiek gecontroleerd op nieuwe advertenties.")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .Build();
    }
}
