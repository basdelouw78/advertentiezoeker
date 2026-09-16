using System.Text.Json;
using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

/// <summary>
/// Eenvoudige JSON-bestandsopslag voor de instellingen. Handig voor desktop/tests; op
/// telefoons kun je beter een implementatie gebruiken die het SMTP-wachtwoord via de
/// beveiligde opslag van het besturingssysteem bewaart (bijv. .NET MAUI's SecureStorage)
/// in plaats van in platte tekst op schijf.
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "instellingen.json");
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettings();
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return settings ?? new AppSettings();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
