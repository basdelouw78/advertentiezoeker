using System.Globalization;

namespace AdvertentieZoeker.Core.Services;

/// <summary>
/// Marktplaats geeft datums terug als relatieve Nederlandse woorden ("Vandaag", "Gisteren",
/// "Eergisteren") of als een korte Nederlandse datum ("10 mrt 24"). Deze parser zet dat om
/// naar een <see cref="DateOnly"/>.
/// </summary>
public static class DutchDateParser
{
    private static readonly Dictionary<string, string> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = "Jan",
        ["feb"] = "Feb",
        ["mrt"] = "Mar",
        ["apr"] = "Apr",
        ["mei"] = "May",
        ["jun"] = "Jun",
        ["jul"] = "Jul",
        ["aug"] = "Aug",
        ["sep"] = "Sep",
        ["okt"] = "Oct",
        ["nov"] = "Nov",
        ["dec"] = "Dec",
    };

    public static bool TryParse(string? text, DateTimeOffset now, out DateOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        switch (text.Trim())
        {
            case "Vandaag":
                result = DateOnly.FromDateTime(now.Date);
                return true;
            case "Gisteren":
                result = DateOnly.FromDateTime(now.Date.AddDays(-1));
                return true;
            case "Eergisteren":
                result = DateOnly.FromDateTime(now.Date.AddDays(-2));
                return true;
        }

        var normalized = text.Trim();
        foreach (var (dutch, english) in MonthMap)
        {
            normalized = normalized.Replace(dutch, english, StringComparison.OrdinalIgnoreCase);
        }

        if (DateOnly.TryParseExact(
                normalized,
                "d MMM yy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
        {
            return true;
        }

        return false;
    }
}
