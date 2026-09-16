using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;
#if ANDROID
using Android.App;
using Android.Content;
using AndroidX.Core.App;
#elif WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

namespace AdvertentieZoeker.App.Services;

/// <summary>
/// Toont een pop-up (lokale) melding op het toestel zelf voor nieuw gevonden advertenties.
/// Gebruikt bewust de platform-eigen notificatie-API's rechtstreeks in plaats van een
/// plugin: community-pakketten voor lokale meldingen trekken via Google Play Services
/// een hele nieuwe generatie AndroidX-bindings binnen die botst met de rest van de
/// (oudere) AndroidX-graph van dit project.
/// </summary>
public sealed class PopupNotifier : INotifier
{
    private const string AndroidChannelId = "advertentiezoeker_gevonden";

    private readonly ISettingsRepository _settingsRepository;
    private static int _notificationId = 1000;

    public PopupNotifier(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, bool isFirstRun, CancellationToken cancellationToken = default)
    {
        if (newListings.Count == 0 || isFirstRun)
        {
            return;
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.PopupNotificationsEnabled)
        {
            return;
        }

        var title = newListings.Count == 1
            ? $"Nieuwe advertentie: {search.Name}"
            : $"{newListings.Count} nieuwe advertenties: {search.Name}";

        var description = newListings.Count == 1
            ? $"{newListings[0].Title} — €{newListings[0].PriceInEuros:0.00}"
            : string.Join(", ", newListings.Take(3).Select(l => l.Title));

        Show(Interlocked.Increment(ref _notificationId), title, description);
    }

    private static void Show(int id, string title, string description)
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O
            && notificationManager.GetNotificationChannel(AndroidChannelId) is null)
        {
            var channel = new NotificationChannel(AndroidChannelId, "Gevonden advertenties", NotificationImportance.Default)
            {
                Description = "Meldingen voor nieuw gevonden Marktplaats-advertenties.",
            };
            notificationManager.CreateNotificationChannel(channel);
        }

        var notification = new NotificationCompat.Builder(context, AndroidChannelId)
            .SetContentTitle(title)
            .SetContentText(description)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .Build();

        try
        {
            NotificationManagerCompat.From(context).Notify(id, notification);
        }
        catch (Java.Lang.SecurityException)
        {
            // Gebruiker heeft de meldingstoestemming (Android 13+) niet gegeven; geen melding tonen.
        }
#elif WINDOWS
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(description)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Windows-toastmeldingen zijn (nog) niet geregistreerd/beschikbaar; niet fataal.
        }
#endif
    }
}
