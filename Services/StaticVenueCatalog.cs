using System.Collections.Generic;
using System.Linq;
using PhiluWedding.Domain;

namespace PhiluWedding.Services;

/// <summary>
/// Canonical, in-memory catalogue of the two wedding venues and their
/// eight banquet halls. Acts as a <strong>read-only fallback</strong> for
/// the public venue hall-selection pages when the database is unavailable
/// or has no rows for a given venue.
///
/// <para>
/// The data here is the same business truth that
/// <see cref="DbSeeder"/> would insert into the database. As long as both
/// stay in sync, the customer-facing experience is identical whether the
/// data comes from PostgreSQL or from this catalogue.
/// </para>
///
/// <para>
/// Rules:
/// <list type="bullet">
///   <item>Slugs are stable URL identifiers (e.g. <c>golden-phoenix-sanh-a</c>).</item>
///   <item>Synthetic ids are negative (and outside the EF Core <c>int</c> primary
///         key range of 1..) so they can never collide with a real database id.</item>
///   <item>Image paths point to the public static assets under
///         <c>wwwroot/images/venues/...</c>. The browser receives clean URLs,
///         never local filesystem paths.</item>
///   <item>Capacities match the supplied business data exactly — no
///         fabricated ranges.</item>
/// </list>
/// </para>
/// </summary>
public static class StaticVenueCatalog
{
    /// <summary>Base offset for synthetic ids. Chosen to be negative so it
    /// can never collide with an EF Core identity-generated positive id.</summary>
    private const int SyntheticIdBase = -1_000_000;

    private static readonly IReadOnlyDictionary<string, StaticHallRecord> _bySlug
        = BuildAll().ToDictionary(r => r.Slug, System.StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<int, StaticHallRecord> _byId
        = BuildAll().ToDictionary(r => r.Id);

    /// <summary>All halls belonging to the given venue key, in stable display order.</summary>
    public static IReadOnlyList<StaticHallRecord> GetHalls(string venueKey)
    {
        return BuildAll().Where(r => r.VenueKey == venueKey).ToList();
    }

    /// <summary>Look up a single hall by its stable slug.</summary>
    public static StaticHallRecord? FindBySlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }
        return _bySlug.TryGetValue(slug, out var record) ? record : null;
    }

    /// <summary>Look up a single hall by its synthetic id.</summary>
    public static StaticHallRecord? FindBySyntheticId(int syntheticId)
    {
        return _byId.TryGetValue(syntheticId, out var record) ? record : null;
    }

    /// <summary>Human-readable venue display name for the given venue key.</summary>
    public static string GetVenueDisplayName(string venueKey) => venueKey switch
    {
        VenueKeys.GoldenPhoenix => "Golden Phoenix",
        VenueKeys.PhiLu => "Trung tâm Hội nghị Tiệc cưới Phì Lũ",
        _ => "Nhà hàng"
    };

    /// <summary>URL slug for the venue landing page.</summary>
    public static string GetVenueSlug(string venueKey)
        => VenueKeys.ToSlug(venueKey) ?? string.Empty;

    /// <summary>Builds the canonical 8-hall list.</summary>
    private static IReadOnlyList<StaticHallRecord> BuildAll()
    {
        var list = new List<StaticHallRecord>(8)
        {
            // ---- Golden Phoenix (4 halls) ----
            new(
                Id: SyntheticIdBase + 1,
                VenueKey: VenueKeys.GoldenPhoenix,
                Name: "Sảnh A",
                Slug: "golden-phoenix-sanh-a",
                CapacityMin: 250,
                CapacityMax: 550,
                ImageUrl: "/images/venues/golden-phoenix/hall-a.webp",
                ImageAlt: "Sảnh A — Golden Phoenix"),

            new(
                Id: SyntheticIdBase + 2,
                VenueKey: VenueKeys.GoldenPhoenix,
                Name: "Sảnh B",
                Slug: "golden-phoenix-sanh-b",
                CapacityMin: 550,
                CapacityMax: 900,
                ImageUrl: "/images/venues/golden-phoenix/hall-b.webp",
                ImageAlt: "Sảnh B — Golden Phoenix"),

            new(
                Id: SyntheticIdBase + 3,
                VenueKey: VenueKeys.GoldenPhoenix,
                Name: "Sảnh C",
                Slug: "golden-phoenix-sanh-c",
                CapacityMin: 250,
                CapacityMax: 550,
                ImageUrl: "/images/venues/golden-phoenix/hall-c.webp",
                ImageAlt: "Sảnh C — Golden Phoenix"),

            new(
                Id: SyntheticIdBase + 4,
                VenueKey: VenueKeys.GoldenPhoenix,
                Name: "Sảnh D",
                Slug: "golden-phoenix-sanh-d",
                CapacityMin: 550,
                CapacityMax: 900,
                ImageUrl: "/images/venues/golden-phoenix/hall-d.webp",
                ImageAlt: "Sảnh D — Golden Phoenix"),

            // ---- Phì Lũ (4 halls) ----
            new(
                Id: SyntheticIdBase + 5,
                VenueKey: VenueKeys.PhiLu,
                Name: "Sảnh Amber",
                Slug: "philu-sanh-amber",
                CapacityMin: 550,
                CapacityMax: 1000,
                ImageUrl: "/images/venues/philu/hall-amber.webp",
                ImageAlt: "Sảnh Amber — Trung tâm Hội nghị Tiệc cưới Phì Lũ"),

            new(
                Id: SyntheticIdBase + 6,
                VenueKey: VenueKeys.PhiLu,
                Name: "Sảnh Pearl",
                Slug: "philu-sanh-pearl",
                CapacityMin: 250,
                CapacityMax: 500,
                ImageUrl: "/images/venues/philu/hall-pearl.webp",
                ImageAlt: "Sảnh Pearl — Trung tâm Hội nghị Tiệc cưới Phì Lũ"),

            new(
                Id: SyntheticIdBase + 7,
                VenueKey: VenueKeys.PhiLu,
                Name: "Sảnh Diamond",
                Slug: "philu-sanh-diamond",
                CapacityMin: 60,
                CapacityMax: 200,
                ImageUrl: "/images/venues/philu/hall-diamond.webp",
                ImageAlt: "Sảnh Diamond — Trung tâm Hội nghị Tiệc cưới Phì Lũ"),

            new(
                Id: SyntheticIdBase + 8,
                VenueKey: VenueKeys.PhiLu,
                Name: "Sảnh Crystal",
                Slug: "philu-sanh-crystal",
                CapacityMin: 50,
                CapacityMax: 150,
                ImageUrl: "/images/venues/philu/hall-crystal.webp",
                ImageAlt: "Sảnh Crystal — Trung tâm Hội nghị Tiệc cưới Phì Lũ"),
        };

        return list;
    }
}

/// <summary>
/// A single canonical hall record from <see cref="StaticVenueCatalog"/>.
/// All data is immutable; instances are shared.
/// </summary>
public sealed record StaticHallRecord(
    int Id,
    string VenueKey,
    string Name,
    string Slug,
    int CapacityMin,
    int CapacityMax,
    string ImageUrl,
    string ImageAlt);
