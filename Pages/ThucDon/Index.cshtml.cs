using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public landing page of the wedding menu.
/// <para>
/// The page shows the menu the way a couple shops for it: the four public
/// price tiers first, the menu options of every tier right below the price
/// they belong to, then the menu sets and the venue price sheets that carry
/// the same information in full.
/// </para>
/// <para>
/// Which tiers and which options are public is decided by
/// <see cref="PublicMenuScope"/> — the same rule the venue price sheets and
/// the menu-set detail page apply. This page never keeps a second list of
/// its own, so it can never show a tier or an option the rest of the site
/// hides.
/// </para>
/// <para>
/// Reads from the database through <see cref="PhiluWeddingDbContext"/>;
/// no caching layer is added at this stage. The page degrades gracefully
/// when the database is unavailable or has no rows yet.
/// </para>
/// </summary>
public class IndexModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>The list of menu sets to render on the page.</summary>
    public IReadOnlyList<MenuSetCardViewModel> MenuSets { get; private set; } =
        Array.Empty<MenuSetCardViewModel>();

    /// <summary>
    /// The public price tiers of the wedding menu, in their configured order.
    /// </summary>
    public IReadOnlyList<MenuTierCardViewModel> Tiers { get; private set; } =
        Array.Empty<MenuTierCardViewModel>();

    /// <summary>
    /// The wedding menu set — the menu the price tiers belong to. It is
    /// <c>null</c> when the catalog holds no wedding menu set yet.
    /// </summary>
    public MenuSetCardViewModel? WeddingMenuSet { get; private set; }

    /// <summary>The number of guests one table is priced for.</summary>
    public int GuestsPerTable => Tiers.Count > 0 ? Tiers[0].GuestsPerTable : 0;

    /// <summary>The number of public menu options across all tiers.</summary>
    public int OptionCount => Tiers.Sum(t => t.Options.Count);

    /// <summary>
    /// True when every tier is priced identically for both venues — the
    /// case the venue price sheets state as well.
    /// </summary>
    public bool IsSharedPricing => Tiers.Count > 0 && Tiers.All(t => t.IsSharedPrice);

    /// <summary>True when the database connection failed; the page shows a polite notice instead of crashing.</summary>
    public bool DatabaseAvailable { get; private set; } = true;

    public IndexModel(
        ILogger<IndexModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        // DbContext is optional: Program.cs only registers it when a
        // connection string is configured. The page degrades gracefully
        // when no context is available.
        _db = db;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogInformation(
                "ThucDon listing: no database context registered, showing empty state.");
            return;
        }

        try
        {
            // The public scope is the same rule the detail pages use, so a
            // combo that is not public never inflates the count of a card.
            var scope = await PublicMenuScope.CreateFilterAsync(_db);

            var countRows = await scope.ApplyTo(_db.MenuCombos.AsNoTracking().Where(c => c.IsPublished))
                .GroupBy(c => c.MenuSetId)
                .Select(g => new { MenuSetId = g.Key, Count = g.Count() })
                .ToListAsync();

            var counts = countRows.ToDictionary(r => r.MenuSetId, r => r.Count);

            var menuSets = await _db.MenuSets
                .AsNoTracking()
                .Where(m => m.IsPublished)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .ToListAsync();

            MenuSets = menuSets.Select(m => new MenuSetCardViewModel(
                m.Id,
                m.Name,
                m.Slug,
                m.ShortDescription,
                m.Description,
                counts.GetValueOrDefault(m.Id))).ToList();

            WeddingMenuSet = MenuSets.FirstOrDefault(
                m => string.Equals(m.Slug, PublicMenuScope.WeddingMenuSetSlug, StringComparison.Ordinal));

            // The price tiers and their options come from the same scope,
            // so the landing page shows exactly the tiers and options the
            // venue price sheets show.
            Tiers = await LoadTiersAsync(scope);
        }
        catch (Exception ex)
        {
            // Any database-level failure (connection refused, missing
            // table, permissions, etc.) is treated as "database
            // unavailable". The page still renders with an empty state
            // and a polite notice, rather than 500.
            //
            // Log only the message — never the full exception object.
            DatabaseAvailable = false;
            _logger.LogWarning(
                "ThucDon listing: database unavailable, showing empty state. {Message}",
                ex.Message);
        }
    }

    // -----------------------------------------------------------------
    // Queries
    // -----------------------------------------------------------------

    /// <summary>
    /// Loads the public price tiers with the public options of every tier.
    /// The options are matched by their tier, so an option that belongs to
    /// no tier can never appear in the tier grid.
    /// </summary>
    private async Task<IReadOnlyList<MenuTierCardViewModel>> LoadTiersAsync(PublicMenuFilter scope)
    {
        var tierIds = scope.Tiers.Select(t => t.Id).ToList();

        if (tierIds.Count == 0)
        {
            return Array.Empty<MenuTierCardViewModel>();
        }

        var optionRows = await scope.ApplyTo(_db!.MenuCombos
                .AsNoTracking()
                .Where(c => c.IsPublished
                            && c.MenuTierId.HasValue
                            && tierIds.Contains(c.MenuTierId!.Value)))
            .OrderBy(c => c.MenuTierId)
            .ThenBy(c => c.OptionSortOrder)
            .ThenBy(c => c.Id)
            .Select(c => new OptionRow(
                c.Id,
                c.MenuTierId!.Value,
                c.OptionCode,
                c.OptionSortOrder,
                c.Name,
                c.ComboDishes.Count(link => link.Dish != null && link.Dish.IsPublished)))
            .ToListAsync();

        var optionsByTier = optionRows
            .GroupBy(o => o.TierId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OptionRow>)g.ToList());

        var tiers = new List<MenuTierCardViewModel>(scope.Tiers.Count);

        foreach (var tier in scope.Tiers)
        {
            optionsByTier.TryGetValue(tier.Id, out var tierOptions);
            tierOptions ??= Array.Empty<OptionRow>();

            var options = tierOptions
                .Where(o => !string.IsNullOrWhiteSpace(o.OptionCode))
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Id)
                .Select(o => new MenuOptionCardViewModel(
                    o.Id,
                    o.OptionCode!.Trim(),
                    o.Name,
                    BuildOptionShortName(o.Name, o.OptionCode!.Trim()),
                    o.SortOrder,
                    o.DishCount))
                .ToList();

            var hasValidPrice = MenuPriceFormatter.IsDisplayable(tier.Price);

            tiers.Add(new MenuTierCardViewModel(
                tier.Id,
                tier.Key,
                tier.Name,
                tier.Slug,
                tier.Description,
                tier.Price,
                tier.GuestsPerTable,
                hasValidPrice ? MenuPriceFormatter.FormatAmount(tier.Price) : null,
                hasValidPrice,
                string.Equals(tier.VenueScope, MenuVenueScopes.BothVenues, StringComparison.Ordinal),
                options));
        }

        return tiers;
    }

    /// <summary>
    /// Builds the short name of a menu option. The stored name repeats the
    /// price tier ("Thực đơn 2.500.000 đồng/bàn - Phương án A"); the page
    /// shows the part after the separator, because the tier is already
    /// visible on the card the option belongs to.
    /// </summary>
    private static string BuildOptionShortName(string? name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return code;
        }

        var separator = name.LastIndexOf(" - ", StringComparison.Ordinal);

        if (separator >= 0 && separator + 3 < name.Length)
        {
            return name[(separator + 3)..].Trim();
        }

        return name.Trim();
    }

    // -----------------------------------------------------------------
    // Projections
    // -----------------------------------------------------------------

    /// <summary>
    /// Flat projection of a menu set plus its published combo count.
    /// Kept as a record so the Razor view is straightforward.
    /// </summary>
    public sealed record MenuSetCardViewModel(
        int Id,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        int ComboCount);

    /// <summary>
    /// Flat projection of a public price tier as the landing page shows it.
    /// </summary>
    public sealed record MenuTierCardViewModel(
        int Id,
        string Key,
        string Name,
        string Slug,
        string? Description,
        decimal Price,
        int GuestsPerTable,
        /// <summary>The formatted price, or null when the stored price is not usable.</summary>
        string? PriceDisplay,
        /// <summary>False when the stored price is missing or zero.</summary>
        bool HasValidPrice,
        /// <summary>True when the tier is priced identically for every venue.</summary>
        bool IsSharedPrice,
        IReadOnlyList<MenuOptionCardViewModel> Options);

    /// <summary>
    /// Flat projection of a public menu option of a price tier.
    /// </summary>
    public sealed record MenuOptionCardViewModel(
        int Id,
        string Code,
        string Name,
        /// <summary>
        /// The name without the price-tier prefix, for example
        /// <c>Phương án A</c> instead of
        /// <c>Thực đơn 2.500.000 đồng/bàn - Phương án A</c>.
        /// </summary>
        string ShortName,
        int SortOrder,
        /// <summary>The number of published dishes of this option.</summary>
        int DishCount);

    /// <summary>One option row of a price tier, as it is read from the database.</summary>
    private sealed record OptionRow(
        int Id,
        int TierId,
        string? OptionCode,
        int SortOrder,
        string Name,
        int DishCount);
}
