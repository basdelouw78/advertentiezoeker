using System.Collections.ObjectModel;
using AdvertentieZoeker.App.Services;

namespace AdvertentieZoeker.App.Views;

public partial class ResultsPage : ContentPage
{
    private readonly FoundListingsLog _log;
    private readonly ObservableCollection<FoundListingEntry> _entries = new();

    public ResultsPage(FoundListingsLog log)
    {
        InitializeComponent();
        _log = log;
        EntriesCollectionView.ItemsSource = _entries;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var recent = await _log.GetRecentAsync();
        _entries.Clear();
        foreach (var entry in recent)
        {
            _entries.Add(entry);
        }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await ReloadAsync();
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Grid { BindingContext: FoundListingEntry entry })
        {
            return;
        }

        await Launcher.Default.OpenAsync(entry.Listing.Url);
    }
}
