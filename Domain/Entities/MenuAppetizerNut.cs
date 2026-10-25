using PhiluWedding.Domain;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// An appetizer nut served at a wedding banquet.
///
/// <para>
/// The source sheet gives no numeric price for these items: two of them are
/// priced by the current market price and four have no price stated at all.
/// No amount is invented — <see cref="Price"/> stays null and
/// <see cref="PriceType"/> records why.
/// </para>
/// </summary>
public class MenuAppetizerNut
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>How the price is quoted — see <see cref="MenuPriceTypes"/>.</summary>
    public string PriceType { get; set; } = MenuPriceTypes.Unspecified;

    /// <summary>Exact amount in VND. Null when the source has no numeric price.</summary>
    public decimal? Price { get; set; }

    /// <summary>Unit the price refers to, when the source states one.</summary>
    public string? Unit { get; set; }

    /// <summary>Extra wording from the source sheet.</summary>
    public string? Note { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.PhiLu;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
