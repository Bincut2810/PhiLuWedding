using System.Collections.Generic;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Flat projections used by the public menu pages.
/// <para>
/// They exist so the Razor views never touch the EF Core entities and never
/// run a query: the page model resolves everything it needs up front and the
/// view only reads these records.
/// </para>
/// </summary>
public static class MenuVenueViewModels
{
    // -----------------------------------------------------------------
    // Price tiers and options
    // -----------------------------------------------------------------

    /// <summary>
    /// One shared wedding-menu price tier. The price lives here and is the
    /// same for every venue.
    /// </summary>
    public sealed record MenuTierViewModel(
        int Id,
        string Key,
        string Name,
        string Slug,
        string? Description,
        decimal Price,
        int GuestsPerTable,
        int SortOrder,
        /// <summary>The formatted price, or null when the stored price is not usable.</summary>
        string? PriceDisplay,
        /// <summary>False when the stored price is missing or zero.</summary>
        bool HasValidPrice,
        /// <summary>The venue scope of the tier — see <c>MenuVenueScopes</c>.</summary>
        string VenueScope,
        /// <summary>True when the tier is priced identically for every venue.</summary>
        bool IsSharedPrice,
        IReadOnlyList<MenuOptionViewModel> Options);

    /// <summary>
    /// One menu option (A, B or C) of a price tier. The option carries the
    /// fixed dish composition; the price comes from its tier.
    /// </summary>
    public sealed record MenuOptionViewModel(
        int Id,
        string Code,
        string Name,
        /// <summary>
        /// The name without the price-tier prefix, for example
        /// <c>Phương án A</c> instead of
        /// <c>Thực đơn 2.200.000 đồng/bàn - Phương án A</c>.
        /// </summary>
        string ShortName,
        int SortOrder,
        IReadOnlyList<MenuDishViewModel> Dishes);

    /// <summary>One dish of a menu option.</summary>
    public sealed record MenuDishViewModel(
        int Id,
        string Name,
        string Slug,
        int Quantity,
        string? Note);

    // -----------------------------------------------------------------
    // Supporting sections
    // -----------------------------------------------------------------

    /// <summary>An additional service or fee.</summary>
    public sealed record MenuServiceViewModel(
        int Id,
        string Name,
        string PriceType,
        /// <summary>The formatted price, or null when the row has no numeric price.</summary>
        string? PriceDisplay,
        /// <summary>The wording shown when there is no numeric price.</summary>
        string? PriceFallback,
        string? Unit,
        string? Note,
        int SortOrder);

    /// <summary>A beverage with its two source price columns.</summary>
    public sealed record MenuBeverageViewModel(
        int Id,
        string Name,
        string? PriceBelowDisplay,
        string? PriceAboveDisplay,
        int ThresholdTableCount,
        string? ThresholdNote,
        string? Unit,
        string? Note,
        int SortOrder);

    /// <summary>An appetizer nut.</summary>
    public sealed record MenuAppetizerNutViewModel(
        int Id,
        string Name,
        string? Description,
        string? PriceDisplay,
        string? PriceFallback,
        string? Unit,
        string? Note,
        int SortOrder);

    /// <summary>
    /// A promotion tier. <see cref="CumulativeBenefits"/> holds every
    /// benefit the customer receives at this tier, inherited ones included.
    /// </summary>
    public sealed record MenuPromotionViewModel(
        int Id,
        string Name,
        string RangeLabel,
        int? MinTableCount,
        int? MaxTableCount,
        bool IncludesPreviousTier,
        /// <summary>The range label of the tier this one inherits from.</summary>
        string? BaseRangeLabel,
        /// <summary>The benefits of this tier only.</summary>
        IReadOnlyList<string> OwnBenefits,
        /// <summary>The benefits of this tier plus every inherited tier.</summary>
        IReadOnlyList<string> CumulativeBenefits,
        int SortOrder);

    /// <summary>A shared condition that applies to the promotion tiers.</summary>
    public sealed record MenuPromotionTermViewModel(
        int Id,
        string Content,
        int SortOrder);

    /// <summary>One deposit milestone of one hall.</summary>
    public sealed record MenuDepositStageViewModel(
        string Label,
        string? AmountDisplay,
        string? Description);

    /// <summary>
    /// The deposit milestones of one hall. The hall is matched by its stable
    /// slug, never by a guessed database id.
    /// </summary>
    public sealed record MenuDepositHallViewModel(
        string? HallSlug,
        string? HallName,
        int? WeddingHallId,
        MenuDepositStageViewModel? First,
        MenuDepositStageViewModel? Second);

    /// <summary>A note about the prices themselves (validity, VAT).</summary>
    public sealed record MenuPriceNoteViewModel(
        int Id,
        string Title,
        string Content,
        System.DateOnly? ValidUntil,
        string? ValidUntilDisplay,
        string? VatNote,
        int SortOrder);
}
