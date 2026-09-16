namespace AdvertentieZoeker.Core.Models;

public sealed record ListingLocation
{
    public string? City { get; init; }
    public string? Country { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    /// <summary>Afstand tot het opgegeven postcodegebied, in kilometers.</summary>
    public double? DistanceKm { get; init; }
}
