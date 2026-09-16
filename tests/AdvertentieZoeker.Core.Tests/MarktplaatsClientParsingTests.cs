using System.Net;
using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;
using Xunit;

namespace AdvertentieZoeker.Core.Tests;

public class MarktplaatsClientParsingTests
{
    private const string FixtureJson = """
    {
      "totalResultCount": 2,
      "listings": [
        {
          "itemId": "m2123456789",
          "title": "Mooie racefiets",
          "description": "Nauwelijks gebruikt",
          "date": "Vandaag",
          "priceInfo": { "priceCents": 15000, "priceType": "FIXED" },
          "location": { "cityName": "Utrecht", "countryName": "Nederland", "latitude": 52.09, "longitude": 5.12, "distanceMeters": 4200 },
          "pictures": [ { "extraSmallUrl": "https://images.marktplaats.com/small.jpg" } ],
          "categoryId": 123
        },
        {
          "itemId": "m2999999999",
          "title": "Kapotte fiets voor onderdelen",
          "date": "10 mrt 24",
          "priceInfo": { "priceCents": 0, "priceType": "SEE_DESCRIPTION" },
          "location": { "cityName": "Amsterdam", "countryName": "Nederland", "latitude": 0, "longitude": 0, "distanceMeters": -1000 },
          "categoryId": 123
        }
      ]
    }
    """;

    [Fact]
    public async Task SearchAsync_ParseertVeldenUitDeJsonRespons()
    {
        var handler = new FakeHttpMessageHandler(FixtureJson);
        var httpClient = new HttpClient(handler);
        var client = new MarktplaatsClient(httpClient);

        var listings = await client.SearchAsync(new SavedSearch { Keywords = "fiets" });

        Assert.Equal(2, listings.Count);

        var first = listings[0];
        Assert.Equal("m2123456789", first.Id);
        Assert.Equal("Mooie racefiets", first.Title);
        Assert.Equal(150.00m, first.PriceInEuros);
        Assert.Equal("FIXED", first.PriceType);
        Assert.Equal("Utrecht", first.Location.City);
        Assert.Equal(4.2, first.Location.DistanceKm);
        Assert.Equal("https://link.marktplaats.nl/m2123456789", first.Url);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), first.Date); // "Vandaag" => datum van vandaag

        var second = listings[1];
        Assert.Equal(new DateOnly(2024, 3, 10), second.Date);
        Assert.Null(second.Location.DistanceKm); // -1000 => onbekend/geen afstand
        Assert.Null(second.Location.Latitude); // 0 => onbekend
    }

    [Fact]
    public async Task SearchAsync_StoptBijEenPaginaKleinerDanDeLimiet()
    {
        var handler = new FakeHttpMessageHandler(FixtureJson);
        var httpClient = new HttpClient(handler);
        var client = new MarktplaatsClient(httpClient);

        await client.SearchAsync(new SavedSearch { Keywords = "fiets" });

        // De fixture bevat maar 2 resultaten (< 100), dus er hoort maar 1 pagina opgevraagd te zijn.
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class FakeHttpMessageHandler(string jsonResponse) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
