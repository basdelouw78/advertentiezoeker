namespace AdvertentieZoeker.Core.Models;

/// <summary>
/// Staat van een advertentie, met de attribuut-ID's die Marktplaats zelf gebruikt
/// in zijn zoek-API (attributesById[]).
/// </summary>
public enum Condition
{
    Nieuw = 30,
    ZoGoedAlsNieuw = 31,
    Gebruikt = 32,
    NietWerkend = 13940,
    Refurbished = 14050,
}

public static class ConditionExtensions
{
    public static string ToDisplayName(this Condition condition) => condition switch
    {
        Condition.Nieuw => "Nieuw",
        Condition.ZoGoedAlsNieuw => "Zo goed als nieuw",
        Condition.Gebruikt => "Gebruikt",
        Condition.NietWerkend => "Niet werkend",
        Condition.Refurbished => "Refurbished",
        _ => condition.ToString(),
    };
}
