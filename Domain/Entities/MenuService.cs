namespace PhiluWedding.Domain.Entities;

/// <summary>
/// An additional service or fee that can be added to a wedding banquet
/// (music, dance performers, decoration handling fees, ...).
///
/// <para>
/// The price semantics of the source sheet are preserved instead of being
/// flattened into a single number:
/// <list type="bullet">
///   <item><description><see cref="MenuPriceTypes.Fixed"/> — one exact amount.</description></item>
///   <item><description><see cref="MenuPriceTypes.From"/> — a starting amount ("từ ...").</description></item>
///   <item><description><see cref="MenuPriceTypes.Range"/> — a minimum and a maximum amount.</description></item>
///   <item><description><see cref="MenuPriceTypes.DependsOnType"/> — no numeric price; depends on the chosen type.</description></item>
///   <item><description><see cref="MenuPriceTypes.Unspecified"/> — the source gives no price at all.</description></item>
/// </list>
/// </para>
/// </summary>
public class MenuService
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>How the price is quoted — see <see cref="MenuPriceTypes"/>.</summary>
    public string PriceType { get; set; } = MenuPriceTypes.Unspecified;

    /// <summary>Exact amount or the lowest amount of a range, in VND. Null when there is no numeric price.</summary>
    public decimal? PriceFrom { get; set; }

    /// <summary>Highest amount of a range, in VND. Null unless <see cref="PriceType"/> is a range.</summary>
    public decimal? PriceTo { get; set; }

    /// <summary>Unit the price refers to (for example "bàn"), when the source states one.</summary>
    public string? Unit { get; set; }

    /// <summary>Extra wording from the source sheet (for example "liên hệ nhân viên để được tư vấn").</summary>
    public string? Note { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.PhiLu;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
