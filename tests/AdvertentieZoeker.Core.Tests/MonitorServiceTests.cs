using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;
using Xunit;

namespace AdvertentieZoeker.Core.Tests;

public class MonitorServiceTests
{
    private static Listing MakeListing(string id) => new()
    {
        Id = id,
        Title = $"Advertentie {id}",
        Location = new ListingLocation(),
        Url = $"https://link.marktplaats.nl/{id}",
    };

    [Fact]
    public async Task CheckAsync_MeldtNietsBijDeAllereersteKeer()
    {
        var client = new FakeMarktplaatsClient([MakeListing("a"), MakeListing("b")]);
        var seenRepo = new InMemorySeenListingRepository();
        var notifier = new RecordingNotifier();
        var monitor = new MonitorService(client, seenRepo, [notifier]);
        var search = new SavedSearch { Keywords = "fiets" };

        var newListings = await monitor.CheckAsync(search);

        Assert.Empty(newListings);
        Assert.Empty(notifier.Calls);
        Assert.Equal(2, (await seenRepo.GetSeenIdsAsync(search.Id)).Count);
    }

    [Fact]
    public async Task CheckAsync_MeldtAlleenNieuweAdvertentiesBijLatereRuns()
    {
        var client = new FakeMarktplaatsClient([MakeListing("a"), MakeListing("b")]);
        var seenRepo = new InMemorySeenListingRepository();
        var notifier = new RecordingNotifier();
        var monitor = new MonitorService(client, seenRepo, [notifier]);
        var search = new SavedSearch { Keywords = "fiets" };

        await monitor.CheckAsync(search); // baseline

        client.Listings = [MakeListing("a"), MakeListing("b"), MakeListing("c")];
        var newListings = await monitor.CheckAsync(search);

        Assert.Single(newListings);
        Assert.Equal("c", newListings[0].Id);
        Assert.Single(notifier.Calls);
        Assert.Equal(["c"], notifier.Calls[0].NewListings.Select(l => l.Id));
    }

    [Fact]
    public async Task CheckAsync_MeldtNietsAlsErGeenNieuweAdvertentiesZijn()
    {
        var client = new FakeMarktplaatsClient([MakeListing("a")]);
        var seenRepo = new InMemorySeenListingRepository();
        var notifier = new RecordingNotifier();
        var monitor = new MonitorService(client, seenRepo, [notifier]);
        var search = new SavedSearch { Keywords = "fiets" };

        await monitor.CheckAsync(search); // baseline
        var newListings = await monitor.CheckAsync(search); // zelfde resultaat nogmaals

        Assert.Empty(newListings);
        Assert.Empty(notifier.Calls);
    }

    [Fact]
    public async Task CheckAllAsync_SlaatUitgeschakeldeZoekopdrachtenOver()
    {
        var client = new FakeMarktplaatsClient([MakeListing("a")]);
        var seenRepo = new InMemorySeenListingRepository();
        var monitor = new MonitorService(client, seenRepo, []);
        var enabled = new SavedSearch { Keywords = "fiets", Enabled = true };
        var disabled = new SavedSearch { Keywords = "bank", Enabled = false };

        var result = await monitor.CheckAllAsync([enabled, disabled]);

        Assert.True(result.ContainsKey(enabled.Id));
        Assert.False(result.ContainsKey(disabled.Id));
    }

    private sealed class FakeMarktplaatsClient(List<Listing> listings) : IMarktplaatsClient
    {
        public List<Listing> Listings { get; set; } = listings;

        public Task<IReadOnlyList<Listing>> SearchAsync(SavedSearch search, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Listing>>(Listings);
    }

    private sealed class InMemorySeenListingRepository : ISeenListingRepository
    {
        private readonly Dictionary<Guid, HashSet<string>> _seen = new();

        public Task<HashSet<string>> GetSeenIdsAsync(Guid savedSearchId, CancellationToken cancellationToken = default)
            => Task.FromResult(_seen.TryGetValue(savedSearchId, out var ids) ? new HashSet<string>(ids) : new HashSet<string>());

        public Task AddSeenIdsAsync(Guid savedSearchId, IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (!_seen.TryGetValue(savedSearchId, out var set))
            {
                set = new HashSet<string>();
                _seen[savedSearchId] = set;
            }

            foreach (var id in ids)
            {
                set.Add(id);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsFirstRunAsync(Guid savedSearchId, CancellationToken cancellationToken = default)
            => Task.FromResult(!_seen.ContainsKey(savedSearchId));
    }

    private sealed class RecordingNotifier : INotifier
    {
        public List<(SavedSearch Search, IReadOnlyList<Listing> NewListings)> Calls { get; } = new();

        public Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, CancellationToken cancellationToken = default)
        {
            Calls.Add((search, newListings));
            return Task.CompletedTask;
        }
    }
}
