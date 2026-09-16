using System.Web;
using AdvertentieZoeker.Core.Models;
using AdvertentieZoeker.Core.Services;
using Xunit;

namespace AdvertentieZoeker.Core.Tests;

public class MarktplaatsClientQueryTests
{
    [Fact]
    public void BuildRequestUri_ZetBasisparametersCorrect()
    {
        var search = new SavedSearch { Keywords = "racefiets" };

        var uri = MarktplaatsClient.BuildRequestUri(search, offset: 0);

        Assert.StartsWith("https://www.marktplaats.nl/lrp/api/search?", uri.ToString());
        AssertQueryValue(uri, "query", "racefiets");
        AssertQueryValue(uri, "searchInTitleAndDescription", "true");
        AssertQueryValue(uri, "offset", "0");
        AssertQueryValue(uri, "distanceMeters", "1000000");
        Assert.DoesNotContain("postcode=", uri.Query);
    }

    [Fact]
    public void BuildRequestUri_ZetPostcodeEnAfstandOm()
    {
        var search = new SavedSearch { Keywords = "kast", ZipCode = "1012AB", MaxDistanceKm = 25 };

        var uri = MarktplaatsClient.BuildRequestUri(search, offset: 0);

        AssertQueryValue(uri, "postcode", "1012AB");
        AssertQueryValue(uri, "distanceMeters", "25000");
    }

    [Fact]
    public void BuildRequestUri_VoegtElkeStaatAlsAparteParameterToe()
    {
        var search = new SavedSearch
        {
            Keywords = "telefoon",
            Conditions = [Condition.Nieuw, Condition.ZoGoedAlsNieuw],
        };

        var uri = MarktplaatsClient.BuildRequestUri(search, offset: 0);

        var values = HttpUtility.ParseQueryString(uri.Query).GetValues("attributesById[]");
        Assert.NotNull(values);
        Assert.Equal(["30", "31"], values);
    }

    [Fact]
    public void BuildRequestUri_GebruiktOffsetVoorPaginering()
    {
        var search = new SavedSearch { Keywords = "bank" };

        var uri = MarktplaatsClient.BuildRequestUri(search, offset: 100);

        AssertQueryValue(uri, "offset", "100");
    }

    private static void AssertQueryValue(Uri uri, string key, string expected)
    {
        var values = HttpUtility.ParseQueryString(uri.Query);
        Assert.Equal(expected, values[key]);
    }
}
