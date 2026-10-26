using System.Collections.Generic;
using PhiluWedding.Domain;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// One wedding-menu price tier — the shared, admin-editable price of a menu
/// package that is served per table of ten guests.
///
/// <para>
/// A tier is the single source of truth for its price. There is exactly one
/// row per tier and it is shared by both venues
/// (<see cref="MenuVenueScopes.BothVenues"/>), so the price is never
/// duplicated per venue.
/// </para>
///
/// <para>
/// The tier holds <em>only</em> the price and its presentation metadata. The
/// fixed dish composition lives on the options (<see cref="MenuCombo"/>) that
/// point at this tier, so changing a price never touches dish data.
/// </para>
/// </summary>
public class MenuTier
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    /// <summary>Display name of the tier, for example "Thực đơn 2.500.000 đồng/bàn".</summary>
    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Price of one table of ten guests, in VND. Stored as numeric(18,0) —
    /// VND has no minor unit, so no fractional part is ever stored.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>Number of guests the price covers (ten, per the source sheet).</summary>
    public int GuestsPerTable { get; set; } = 10;

    public int SortOrder { get; set; }

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.BothVenues;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>The menu options (A, B, C) that are sold at this price.</summary>
    public ICollection<MenuCombo> Options { get; set; } = new List<MenuCombo>();
}
