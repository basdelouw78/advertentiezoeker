using AdvertentieZoeker.Core.Services;
using Xunit;

namespace AdvertentieZoeker.Core.Tests;

public class DutchDateParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Vandaag", 2026, 9, 16)]
    [InlineData("Gisteren", 2026, 9, 15)]
    [InlineData("Eergisteren", 2026, 9, 14)]
    [InlineData("10 mrt 24", 2024, 3, 10)]
    [InlineData("1 okt 25", 2025, 10, 1)]
    public void TryParse_HerkentBekendeFormaten(string input, int year, int month, int day)
    {
        var success = DutchDateParser.TryParse(input, Now, out var result);

        Assert.True(success);
        Assert.Equal(new DateOnly(year, month, day), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("onzin")]
    public void TryParse_GeeftFalseVoorOnbekendeInvoer(string? input)
    {
        var success = DutchDateParser.TryParse(input, Now, out _);

        Assert.False(success);
    }
}
