using System.Text;
using System.Text.Json;
using AdvertentieZoeker.Core.Models;

namespace AdvertentieZoeker.Core.Services;

/// <summary>
/// Client voor de (onofficiële, niet-gedocumenteerde) zoek-API die marktplaats.nl
/// zelf gebruikt: https://www.marktplaats.nl/lrp/api/search.
/// De structuur van deze API kan zonder aankondiging wijzigen.
/// </summary>
public sealed class MarktplaatsClient : IMarktplaatsClient
{
    private const string SearchUrl = "https://www.marktplaats.nl/lrp/api/search";
    private const int ResultsPerPage = 100;
    private const int MaxPages = 5;

    private readonly HttpClient _httpClient;

    public MarktplaatsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        }
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<IReadOnlyList<Listing>> SearchAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        var results = new List<Listing>();
        var now = DateTimeOffset.Now;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = BuildRequestUri(search, offset: page * ResultsPerPage);
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!document.RootElement.TryGetProperty("listings", out var listingsElement))
            {
                break;
            }

            var pageCount = 0;
            foreach (var listingElement in listingsElement.EnumerateArray())
            {
                results.Add(ParseListing(listingElement, now));
                pageCount++;
            }

            var totalResultCount = document.RootElement.TryGetProperty("totalResultCount", out var totalElement)
                ? totalElement.GetInt32()
                : results.Count;

            if (pageCount < ResultsPerPage || results.Count >= totalResultCount)
            {
                break;
            }
        }

        return results;
    }

    internal static Uri BuildRequestUri(SavedSearch search, int offset)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("query", search.Keywords ?? string.Empty),
            new("searchInTitleAndDescription", "true"),
            new("viewOptions", "list-view"),
            new("limit", ResultsPerPage.ToString()),
            new("offset", offset.ToString()),
            new("sortBy", "SORT_INDEX"),
            new("sortOrder", "DECREASING"),
        };

        if (!string.IsNullOrWhiteSpace(search.ZipCode))
        {
            parameters.Add(new("postcode", search.ZipCode));
        }

        var distanceMeters = search.MaxDistanceKm is > 0
            ? search.MaxDistanceKm.Value * 1000
            : 1_000_000; // "onbeperkt", zoals marktplaats.nl zelf ook doet.
        parameters.Add(new("distanceMeters", distanceMeters.ToString()));

        // attributesById[] moet meerdere keren als losse parameter voorkomen (één per staat).
        if (search.Conditions is { Count: > 0 })
        {
            foreach (var condition in search.Conditions)
            {
                parameters.Add(new("attributesById[]", ((int)condition).ToString()));
            }
        }

        var builder = new StringBuilder(SearchUrl).Append('?');
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            var (key, value) = parameters[i];
            builder.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
        }

        return new Uri(builder.ToString());
    }

    private static Listing ParseListing(JsonElement element, DateTimeOffset now)
    {
        var itemId = element.GetProperty("itemId").GetString() ?? string.Empty;

        DateOnly? date = null;
        if (element.TryGetProperty("date", out var dateElement)
            && DutchDateParser.TryParse(dateElement.GetString(), now, out var parsedDate))
        {
            date = parsedDate;
        }

        var priceCents = 0L;
        var priceType = "UNKNOWN";
        if (element.TryGetProperty("priceInfo", out var priceInfo))
        {
            if (priceInfo.TryGetProperty("priceCents", out var priceCentsElement))
            {
                priceCents = priceCentsElement.GetInt64();
            }
            if (priceInfo.TryGetProperty("priceType", out var priceTypeElement))
            {
                priceType = priceTypeElement.GetString() ?? priceType;
            }
        }

        var location = ParseLocation(element.TryGetProperty("location", out var locationElement) ? locationElement : default);

        string? imageUrl = null;
        if (element.TryGetProperty("pictures", out var pictures) && pictures.GetArrayLength() > 0)
        {
            var firstPicture = pictures[0];
            if (firstPicture.TryGetProperty("extraSmallUrl", out var smallUrl))
            {
                imageUrl = smallUrl.GetString();
            }
            else if (firstPicture.TryGetProperty("mediumUrl", out var mediumUrl))
            {
                imageUrl = mediumUrl.GetString();
            }
        }

        return new Listing
        {
            Id = itemId,
            Title = element.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? string.Empty : string.Empty,
            Description = element.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : null,
            Date = date,
            Location = location,
            PriceCents = priceCents,
            PriceType = priceType,
            Url = "https://link.marktplaats.nl/" + itemId,
            ImageUrl = imageUrl,
        };
    }

    private static ListingLocation ParseLocation(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new ListingLocation();
        }

        double? latitude = element.TryGetProperty("latitude", out var latElement) && latElement.ValueKind == JsonValueKind.Number
            ? latElement.GetDouble()
            : null;
        if (latitude == 0)
        {
            latitude = null;
        }

        double? longitude = element.TryGetProperty("longitude", out var lonElement) && lonElement.ValueKind == JsonValueKind.Number
            ? lonElement.GetDouble()
            : null;
        if (longitude == 0)
        {
            longitude = null;
        }

        double? distanceKm = null;
        if (element.TryGetProperty("distanceMeters", out var distanceElement) && distanceElement.ValueKind == JsonValueKind.Number)
        {
            var meters = distanceElement.GetDouble();
            if (meters is not -1000)
            {
                distanceKm = meters / 1000d;
            }
        }

        return new ListingLocation
        {
            City = element.TryGetProperty("cityName", out var cityElement) ? cityElement.GetString() : null,
            Country = element.TryGetProperty("countryName", out var countryElement) ? countryElement.GetString() : null,
            Latitude = latitude,
            Longitude = longitude,
            DistanceKm = distanceKm,
        };
    }
}
