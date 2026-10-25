using System.Collections.Generic;
using PhiluWedding.Domain;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A beverage sold by the unit during a wedding banquet.
///
/// <para>
/// The source sheet quotes two prices per beverage: one for banquets of fewer
/// than 25 tables and one for more than 25 tables. Both are stored as separate
/// columns so neither is lost. The threshold itself is kept in
/// <see cref="ThresholdTableCount"/> together with the original wording in
/// <see cref="ThresholdNote"/>, because the source does not say what happens
/// for exactly 25 tables.
/// </para>
/// </summary>
public class MenuBeverage
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>Price for a banquet of fewer than <see cref="ThresholdTableCount"/> tables, in VND.</summary>
    public decimal? PriceBelowThreshold { get; set; }

    /// <summary>Price for a banquet of more than <see cref="ThresholdTableCount"/> tables, in VND.</summary>
    public decimal? PriceAboveThreshold { get; set; }

    /// <summary>Table count that separates the two source prices (25).</summary>
    public int ThresholdTableCount { get; set; } = 25;

    /// <summary>
    /// The threshold wording exactly as the source states it, plus a note that
    /// the price for exactly the threshold table count is not specified.
    /// </summary>
    public string? ThresholdNote { get; set; }

    /// <summary>Unit the price refers to, for example "lon" or "chai".</summary>
    public string? Unit { get; set; }

    public string? Note { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.PhiLu;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
