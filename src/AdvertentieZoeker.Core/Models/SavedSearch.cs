namespace AdvertentieZoeker.Core.Models;

/// <summary>Een opgeslagen zoekopdracht die periodiek wordt uitgevoerd.</summary>
public sealed class SavedSearch
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Herkenbare naam, bijvoorbeeld "Racefiets Amsterdam".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Steekwoorden zoals je ze ook in de Marktplaats-zoekbalk zou typen.</summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>Postcode van het gebied waar de afstand vanaf gemeten wordt, bijv. "1012AB".</summary>
    public string? ZipCode { get; set; }

    /// <summary>Maximale afstand tot <see cref="ZipCode"/> in kilometers. Null/leeg = heel Nederland.</summary>
    public int? MaxDistanceKm { get; set; }

    /// <summary>Toegestane staat/condities. Leeg = alle staten.</summary>
    public List<Condition> Conditions { get; set; } = new();

    public bool Enabled { get; set; } = true;
}
