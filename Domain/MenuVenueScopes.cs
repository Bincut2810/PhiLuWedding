namespace PhiluWedding.Domain;

/// <summary>
/// Venue applicability for menu content.
///
/// <para>
/// The wedding menu price tiers are shared by both venues. Every other menu
/// section (services, beverages, appetizer nuts, promotions, deposit terms)
/// comes from the Phì Lũ source price sheet, so those rows are scoped to
/// <see cref="PhiLu"/> until the business confirms that they also apply to
/// Golden Phoenix.
/// </para>
///
/// <para>
/// <see cref="BothVenues"/> means "confirmed for both venues". It never means
/// "unknown" — a row whose applicability is unknown stays scoped to the
/// single venue the source document belongs to until it is confirmed.
/// </para>
/// </summary>
public static class MenuVenueScopes
{
    /// <summary>Trung tâm Hội nghị Tiệc cưới Phì Lũ.</summary>
    public const string PhiLu = "PhiLu";

    /// <summary>Golden Phoenix.</summary>
    public const string GoldenPhoenix = "GoldenPhoenix";

    /// <summary>Confirmed for both venues (shared menu price tiers).</summary>
    public const string BothVenues = "Both";

    /// <summary>True when <paramref name="scope"/> is a recognised scope.</summary>
    public static bool IsKnown(string? scope) =>
        string.Equals(scope, PhiLu, System.StringComparison.Ordinal)
        || string.Equals(scope, GoldenPhoenix, System.StringComparison.Ordinal)
        || string.Equals(scope, BothVenues, System.StringComparison.Ordinal);

    /// <summary>True when the scope covers both venues.</summary>
    public static bool IsBoth(string? scope) =>
        string.Equals(scope, BothVenues, System.StringComparison.Ordinal);
}
