namespace PhiluWedding.Domain;

/// <summary>
/// Stable identifiers for the two wedding venues at Phì Lũ. Used as
/// discriminator values on <c>WeddingHall.VenueKey</c> and as URL slugs
/// for the venue landing pages. Keep these in sync with the route
/// segments under <c>Pages/TiecCuoi/*.cshtml</c>.
/// </summary>
public static class VenueKeys
{
    public const string GoldenPhoenix = "GoldenPhoenix";

    public const string PhiLu = "PhiLu";

    /// <summary>URL slug for the Golden Phoenix venue page.</summary>
    public const string GoldenPhoenixSlug = "golden-phoenix";

    /// <summary>URL slug for the Phì Lũ venue page.</summary>
    public const string PhiLuSlug = "philu";

    /// <summary>True when <paramref name="key"/> is a recognised venue.</summary>
    public static bool IsKnown(string? key) =>
        string.Equals(key, GoldenPhoenix, System.StringComparison.Ordinal)
        || string.Equals(key, PhiLu, System.StringComparison.Ordinal);

    /// <summary>Returns the public URL slug for a venue key, or null when unknown.</summary>
    public static string? ToSlug(string? key) => key switch
    {
        GoldenPhoenix => GoldenPhoenixSlug,
        PhiLu => PhiLuSlug,
        _ => null,
    };

    /// <summary>Returns the venue key for a URL slug, or null when not a venue slug.</summary>
    public static string? FromSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }
        if (string.Equals(slug, GoldenPhoenixSlug, System.StringComparison.OrdinalIgnoreCase))
        {
            return GoldenPhoenix;
        }
        if (string.Equals(slug, PhiLuSlug, System.StringComparison.OrdinalIgnoreCase))
        {
            return PhiLu;
        }
        return null;
    }
}
