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
    // Price notes
    // -----------------------------------------------------------------

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
