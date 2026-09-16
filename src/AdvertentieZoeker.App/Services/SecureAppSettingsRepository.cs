using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Services;

/// <summary>
/// Bewaart de app-instellingen zoals <see cref="JsonSettingsRepository"/> (gewoon bestand),
/// behalve het SMTP-wachtwoord: dat gaat via de beveiligde opslag van het besturingssysteem
/// (Android Keystore / Windows DPAPI via .NET MAUI's <see cref="SecureStorage"/>) zodat het
/// nooit in platte tekst op schijf staat.
/// </summary>
public sealed class SecureAppSettingsRepository : ISettingsRepository
{
    private const string PasswordKey = "smtp_wachtwoord";

    private readonly JsonSettingsRepository _inner;

    public SecureAppSettingsRepository(string directory)
    {
        _inner = new JsonSettingsRepository(directory);
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _inner.GetAsync(cancellationToken).ConfigureAwait(false);
        settings.Email.Password = await SecureStorage.Default.GetAsync(PasswordKey).ConfigureAwait(false) ?? string.Empty;
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var password = settings.Email.Password;

        if (string.IsNullOrEmpty(password))
        {
            SecureStorage.Default.Remove(PasswordKey);
        }
        else
        {
            await SecureStorage.Default.SetAsync(PasswordKey, password).ConfigureAwait(false);
        }

        settings.Email.Password = string.Empty;
        try
        {
            await _inner.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            settings.Email.Password = password;
        }
    }
}
