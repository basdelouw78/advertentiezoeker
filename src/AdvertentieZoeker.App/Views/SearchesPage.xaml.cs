using System.Collections.ObjectModel;
using AdvertentieZoeker.App.Services;
using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Views;

/// <summary>Koppelt een <see cref="SavedSearch"/> aan de status van zijn laatste controle, voor de lijstweergave.</summary>
public sealed class SavedSearchRow
{
    public required SavedSearch Search { get; init; }
    public string StatusText { get; init; } = "Nog niet gecontroleerd.";
}

public partial class SearchesPage : ContentPage
{
    private readonly ISavedSearchRepository _repository;
    private readonly PollingService _pollingService;
    private readonly CheckStatusLog _checkStatusLog;
    private readonly ObservableCollection<SavedSearchRow> _searches = new();

    public SearchesPage(ISavedSearchRepository repository, PollingService pollingService, CheckStatusLog checkStatusLog)
    {
        InitializeComponent();
        _repository = repository;
        _pollingService = pollingService;
        _checkStatusLog = checkStatusLog;
        SearchesCollectionView.ItemsSource = _searches;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var searches = await _repository.GetAllAsync();
        var status = await _checkStatusLog.GetAsync();

        _searches.Clear();
        foreach (var search in searches)
        {
            _searches.Add(new SavedSearchRow { Search = search, StatusText = BuildStatusText(search, status) });
        }

        RefreshOverallStatus(status);
    }

    private static string BuildStatusText(SavedSearch search, CheckStatus? overallStatus)
    {
        var perSearch = overallStatus?.PerSearch?.FirstOrDefault(s => s.SearchId == search.Id);
        if (perSearch is null)
        {
            return "Nog niet gecontroleerd.";
        }

        if (perSearch.ErrorMessage is not null)
        {
            return $"Mislukt: {perSearch.ErrorMessage}";
        }

        return perSearch.NewListingsCount switch
        {
            0 => "Laatste controle: niets nieuws.",
            1 => "Laatste controle: 1 nieuwe advertentie.",
            _ => $"Laatste controle: {perSearch.NewListingsCount} nieuwe advertenties.",
        };
    }

    private void RefreshOverallStatus(CheckStatus? status)
    {
        if (status is null)
        {
            StatusLabel.Text = "Nog niet gecontroleerd.";
        }
        else if (status.Success)
        {
            StatusLabel.Text = status.NewListingsCount switch
            {
                0 => $"Laatste controle: {status.CheckedAt:dd-MM HH:mm} — niets nieuws.",
                1 => $"Laatste controle: {status.CheckedAt:dd-MM HH:mm} — 1 nieuwe advertentie.",
                _ => $"Laatste controle: {status.CheckedAt:dd-MM HH:mm} — {status.NewListingsCount} nieuwe advertenties.",
            };
        }
        else
        {
            StatusLabel.Text = $"Laatste controle ({status.CheckedAt:dd-MM HH:mm}) mislukt: {status.ErrorMessage}";
        }
    }

    private async void OnCheckNowClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Bezig met controleren...";
        try
        {
            await _pollingService.RunOnceAsync();
        }
        catch (Exception ex)
        {
            await _checkStatusLog.RecordFailureAsync(ex.Message);
        }

        await ReloadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await ReloadAsync();
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SearchEditPage));
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Grid { BindingContext: SavedSearchRow row })
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(SearchEditPage)}?id={row.Search.Id}");
    }

    private async void OnEnabledToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: SavedSearchRow row })
        {
            return;
        }

        var all = await _repository.GetAllAsync();
        var stored = all.FirstOrDefault(s => s.Id == row.Search.Id);
        if (stored is not null)
        {
            stored.Enabled = e.Value;
            await _repository.SaveAllAsync(all);
        }
    }

    private async void OnDeleteSwiped(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem { BindingContext: SavedSearchRow row })
        {
            return;
        }

        var confirmed = await DisplayAlert("Verwijderen", $"Zoekopdracht \"{row.Search.Name}\" verwijderen?", "Verwijder", "Annuleer");
        if (!confirmed)
        {
            return;
        }

        var all = await _repository.GetAllAsync();
        all.RemoveAll(s => s.Id == row.Search.Id);
        await _repository.SaveAllAsync(all);
        await ReloadAsync();
    }
}
