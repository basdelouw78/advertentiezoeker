namespace AdvertentieZoeker.Core.Models;

public sealed class AppSettings
{
    public int PollIntervalMinutes { get; set; } = 30;
    public bool PopupNotificationsEnabled { get; set; } = true;
    public EmailSettings Email { get; set; } = new();
}
