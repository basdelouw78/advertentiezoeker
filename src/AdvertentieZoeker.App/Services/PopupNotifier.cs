using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;
using Plugin.LocalNotification;

namespace AdvertentieZoeker.App.Services;

/// <summary>Toont een pop-up (lokale) melding op het toestel zelf voor nieuw gevonden advertenties.</summary>
public sealed class PopupNotifier : INotifier
{
    private readonly ISettingsRepository _settingsRepository;
    private static int _notificationId = 1000;

    public PopupNotifier(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, CancellationToken cancellationToken = default)
    {
        if (newListings.Count == 0)
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

        var request = new NotificationRequest
        {
            NotificationId = Interlocked.Increment(ref _notificationId),
            Title = title,
            Description = description,
            ReturningData = newListings[0].Url,
        };

        await LocalNotificationCenter.Current.Show(request);
    }
}
