using System.Text.Json;
using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Services;

/// <summary>Houdt een korte geschiedenis bij van gevonden advertenties, voor de resultatenpagina.</summary>
public sealed class FoundListingsLog : INotifier
{
    private const int MaxEntries = 200;
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FoundListingsLog(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "gevonden.json");
    }

    public async Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, bool isFirstRun, CancellationToken cancellationToken = default)
    {
        // Ook bij de allereerste controle loggen: dat geeft direct zichtbaar bewijs dat de
        // zoekopdracht werkt, zonder dat de gebruiker daarvoor met pop-ups/e-mail wordt
        // lastiggevallen voor advertenties die er al lang stonden (dat slaan EmailNotifier
        // en PopupNotifier bewust over bij isFirstRun).
        if (newListings.Count == 0)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await ReadAsync(cancellationToken).ConfigureAwait(false);
            foreach (var listing in newListings)
            {
                entries.Insert(0, new FoundListingEntry(search.Name, listing, DateTimeOffset.Now));
            }

            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }

            var json = JsonSerializer.Serialize(entries);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<FoundListingEntry>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<FoundListingEntry>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new List<FoundListingEntry>();
        }

        await using var stream = File.OpenRead(_filePath);
        var entries = await JsonSerializer.DeserializeAsync<List<FoundListingEntry>>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return entries ?? new List<FoundListingEntry>();
    }
}

public sealed record FoundListingEntry(string SearchName, Listing Listing, DateTimeOffset FoundAt);
