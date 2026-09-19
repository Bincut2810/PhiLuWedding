using PhiluWedding.Domain;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Small helper that returns the public URL of the hall-selection page
/// for a given wedding venue. Shared between the database-backed
/// <c>Details</c> page and the static-catalogue-backed <c>Hall</c> page
/// so the breadcrumb / "Quay lại" link always points at the right venue.
/// </summary>
internal static class StaticVenueHrefResolver
{
    /// <summary>Resolve a venue landing-page URL for the given venue key.</summary>
    public static string ResolveForVenue(string? venueKey)
    {
        if (string.Equals(venueKey, VenueKeys.GoldenPhoenix, System.StringComparison.Ordinal))
        {
            return "/tiec-cuoi/golden-phoenix";
        }
        if (string.Equals(venueKey, VenueKeys.PhiLu, System.StringComparison.Ordinal))
        {
            return "/tiec-cuoi/philu";
        }
        return "/TiecCuoi";
    }

    /// <summary>Resolve a venue landing-page URL from a hall slug (static catalogue).</summary>
    public static string ResolveForSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "/TiecCuoi";
        }
        if (slug.StartsWith("golden-phoenix-", System.StringComparison.OrdinalIgnoreCase))
        {
            return "/tiec-cuoi/golden-phoenix";
        }
        if (slug.StartsWith("philu-", System.StringComparison.OrdinalIgnoreCase))
        {
            return "/tiec-cuoi/philu";
        }
        return "/TiecCuoi";
    }
}
