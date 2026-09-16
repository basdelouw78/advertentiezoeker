using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Views;

[QueryProperty(nameof(SearchId), "id")]
public partial class SearchEditPage : ContentPage
{
    private readonly ISavedSearchRepository _repository;
    private Guid? _editingId;

    public string? SearchId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _editingId = id;
                _ = LoadAsync(id);
            }
        }
    }

    public SearchEditPage(ISavedSearchRepository repository)
    {
        InitializeComponent();
        _repository = repository;
    }

    private async Task LoadAsync(Guid id)
    {
        var all = await _repository.GetAllAsync();
        var search = all.FirstOrDefault(s => s.Id == id);
        if (search is null)
        {
            return;
        }

        NameEntry.Text = search.Name;
        KeywordsEntry.Text = search.Keywords;
        ZipCodeEntry.Text = search.ZipCode;
        DistanceEntry.Text = search.MaxDistanceKm?.ToString();
        EnabledSwitch.IsToggled = search.Enabled;
        DeleteButton.IsVisible = true;

        NieuwCheckBox.IsChecked = search.Conditions.Contains(Condition.Nieuw);
        ZoGoedAlsNieuwCheckBox.IsChecked = search.Conditions.Contains(Condition.ZoGoedAlsNieuw);
        GebruiktCheckBox.IsChecked = search.Conditions.Contains(Condition.Gebruikt);
        RefurbishedCheckBox.IsChecked = search.Conditions.Contains(Condition.Refurbished);
        NietWerkendCheckBox.IsChecked = search.Conditions.Contains(Condition.NietWerkend);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) || string.IsNullOrWhiteSpace(KeywordsEntry.Text))
        {
            await DisplayAlert("Ontbrekende gegevens", "Vul minimaal een naam en steekwoorden in.", "Ok");
            return;
        }

        int? maxDistanceKm = null;
        if (!string.IsNullOrWhiteSpace(DistanceEntry.Text))
        {
            if (!int.TryParse(DistanceEntry.Text, out var parsedDistance) || parsedDistance <= 0)
            {
                await DisplayAlert("Ongeldige afstand", "Vul een geheel getal groter dan 0 in, of laat het veld leeg.", "Ok");
                return;
            }
            maxDistanceKm = parsedDistance;
        }

        var conditions = new List<Condition>();
        if (NieuwCheckBox.IsChecked) conditions.Add(Condition.Nieuw);
        if (ZoGoedAlsNieuwCheckBox.IsChecked) conditions.Add(Condition.ZoGoedAlsNieuw);
        if (GebruiktCheckBox.IsChecked) conditions.Add(Condition.Gebruikt);
        if (RefurbishedCheckBox.IsChecked) conditions.Add(Condition.Refurbished);
        if (NietWerkendCheckBox.IsChecked) conditions.Add(Condition.NietWerkend);

        var all = await _repository.GetAllAsync();
        var existing = _editingId is { } id ? all.FirstOrDefault(s => s.Id == id) : null;

        if (existing is null)
        {
            existing = new SavedSearch();
            all.Add(existing);
        }

        existing.Name = NameEntry.Text.Trim();
        existing.Keywords = KeywordsEntry.Text.Trim();
        existing.ZipCode = string.IsNullOrWhiteSpace(ZipCodeEntry.Text) ? null : ZipCodeEntry.Text.Trim();
        existing.MaxDistanceKm = maxDistanceKm;
        existing.Conditions = conditions;
        existing.Enabled = EnabledSwitch.IsToggled;

        await _repository.SaveAllAsync(all);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_editingId is not { } id)
        {
            return;
        }

        var confirmed = await DisplayAlert("Verwijderen", "Deze zoekopdracht verwijderen?", "Verwijder", "Annuleer");
        if (!confirmed)
        {
            return;
        }

        var all = await _repository.GetAllAsync();
        all.RemoveAll(s => s.Id == id);
        await _repository.SaveAllAsync(all);
        await Shell.Current.GoToAsync("..");
    }
}
