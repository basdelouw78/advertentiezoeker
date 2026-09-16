using System.Collections.ObjectModel;
using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Views;

public partial class SearchesPage : ContentPage
{
    private readonly ISavedSearchRepository _repository;
    private readonly ObservableCollection<SavedSearch> _searches = new();

    public SearchesPage(ISavedSearchRepository repository)
    {
        InitializeComponent();
        _repository = repository;
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
        _searches.Clear();
        foreach (var search in searches)
        {
            _searches.Add(search);
        }
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
        if (sender is not Grid { BindingContext: SavedSearch search })
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(SearchEditPage)}?id={search.Id}");
    }

    private async void OnEnabledToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: SavedSearch search })
        {
            return;
        }

        search.Enabled = e.Value;
        var all = await _repository.GetAllAsync();
        var stored = all.FirstOrDefault(s => s.Id == search.Id);
        if (stored is not null)
        {
            stored.Enabled = e.Value;
            await _repository.SaveAllAsync(all);
        }
    }

    private async void OnDeleteSwiped(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem { BindingContext: SavedSearch search })
        {
            return;
        }

        var confirmed = await DisplayAlert("Verwijderen", $"Zoekopdracht \"{search.Name}\" verwijderen?", "Verwijder", "Annuleer");
        if (!confirmed)
        {
            return;
        }

        var all = await _repository.GetAllAsync();
        all.RemoveAll(s => s.Id == search.Id);
        await _repository.SaveAllAsync(all);
        await ReloadAsync();
    }
}
