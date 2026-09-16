using System.Text.Json;

namespace AdvertentieZoeker.Core.Services;

/// <summary>
/// Bewaart per zoekopdracht een JSON-bestand met gezien advertentie-ID's, zodat de app
/// na een herstart weet welke advertenties al gemeld zijn.
/// </summary>
public sealed class JsonSeenListingRepository : ISeenListingRepository
{
    private readonly string _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonSeenListingRepository(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    private string GetFilePath(Guid savedSearchId) => Path.Combine(_directory, $"{savedSearchId:N}.json");

    public async Task<HashSet<string>> GetSeenIdsAsync(Guid savedSearchId, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(savedSearchId);
        if (!File.Exists(path))
        {
            return new HashSet<string>();
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.OpenRead(path);
            var ids = await JsonSerializer.DeserializeAsync<HashSet<string>>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ids ?? new HashSet<string>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<bool> IsFirstRunAsync(Guid savedSearchId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!File.Exists(GetFilePath(savedSearchId)));
    }

    public async Task AddSeenIdsAsync(Guid savedSearchId, IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(savedSearchId);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = File.Exists(path)
                ? JsonSerializer.Deserialize<HashSet<string>>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false))
                  ?? new HashSet<string>()
                : new HashSet<string>();

            foreach (var id in ids)
            {
                existing.Add(id);
            }

            var json = JsonSerializer.Serialize(existing);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
