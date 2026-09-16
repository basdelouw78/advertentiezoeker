using System.Text.Json;
using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

public sealed class JsonSavedSearchRepository : ISavedSearchRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonSavedSearchRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "zoekopdrachten.json");
    }

    public async Task<List<SavedSearch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new List<SavedSearch>();
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.OpenRead(_filePath);
            var searches = await JsonSerializer.DeserializeAsync<List<SavedSearch>>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return searches ?? new List<SavedSearch>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAllAsync(List<SavedSearch> searches, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(searches, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
