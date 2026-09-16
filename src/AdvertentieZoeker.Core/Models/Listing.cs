namespace AdvertentieZoeker.Core.Models;

public sealed record Listing
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateOnly? Date { get; init; }
    public required ListingLocation Location { get; init; }
    public long PriceCents { get; init; }
    public string PriceType { get; init; } = "UNKNOWN";
    public required string Url { get; init; }
    public string? ImageUrl { get; init; }

    public decimal PriceInEuros => PriceCents / 100m;
}
