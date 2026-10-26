using System.Data.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;
using MenuViewModels = PhiluWedding.Pages.ThucDon.MenuVenueViewModels;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Shared page model behind both public wedding-menu pages
/// (<c>/thuc-don/philu</c> and <c>/thuc-don/golden-phoenix</c>).
///
/// <para>
/// <b>What is shared and what is not.</b> The wedding menu has four price
/// tiers and nine menu options in total. They come from one shared catalog,
/// so both venues read exactly the same tier rows and therefore exactly the
/// same prices. Which tiers and options are public is decided by
/// <see cref="PublicMenuScope"/>, the single rule every public menu route
/// applies.
/// </para>
///
/// <para>
/// <b>Read-only.</b> The page never writes, never migrates and never seeds.
/// A visitor opening the menu must not be able to change the catalog.
/// </para>
///
/// <para>
/// <b>Failure handling.</b> A missing database registration, a missing table
/// (the Phase 02A migration has not been applied yet) and a failing query
/// all end in <see cref="DatabaseAvailable"/> being false, which the view
/// renders as a branded "menu data temporarily unavailable" state. A
/// reachable but empty catalog is a different state
/// (<see cref="CatalogEmpty"/>) and is never confused with a failure.
/// </para>
/// </summary>
public abstract class MenuVenuePageModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly ILogger _logger;

    protected MenuVenuePageModel(ILogger logger, PhiluWeddingDbContext? db = null)
    {
        _logger = logger;
        _db = db;
    }

    // -----------------------------------------------------------------
    // Venue identity
    // -----------------------------------------------------------------

    /// <summary>The <see cref="VenueKeys"/> value of the venue this page shows.</summary>
    protected abstract string VenueKey { get; }

    /// <summary>The display name of the venue.</summary>
    protected abstract string VenueDisplayName { get; }

    /// <summary>A short, neutral introduction of the venue's menu.</summary>
    protected abstract string VenueIntroText { get; }

    /// <summary>The Razor page name of the venue page, used for links.</summary>
    protected abstract string VenuePageName { get; }

    /// <summary>The display name of the other venue of the pair.</summary>
    protected abstract string OtherVenueDisplayName { get; }

    /// <summary>The Razor page name of the other venue page.</summary>
    protected abstract string OtherVenuePageName { get; }

    /// <summary>The display name of the venue.</summary>
    public string VenueName => VenueDisplayName;

    /// <summary>The short introduction of the venue's menu.</summary>
    public string VenueIntro => VenueIntroText;

    /// <summary>The Razor page name of this venue page.</summary>
    public string VenuePage => VenuePageName;

    /// <summary>The display name of the other venue of the pair.</summary>
    public string OtherVenueName => OtherVenueDisplayName;

    /// <summary>The Razor page name of the other venue page.</summary>
    public string OtherVenuePage => OtherVenuePageName;

    // -----------------------------------------------------------------
    // Page state
    // -----------------------------------------------------------------

    /// <summary>The public price tiers, in their configured order.</summary>
    public IReadOnlyList<MenuViewModels.MenuTierViewModel> Tiers { get; private set; } =
        Array.Empty<MenuViewModels.MenuTierViewModel>();

    /// <summary>The tier the visitor is currently looking at.</summary>
    public MenuViewModels.MenuTierViewModel? SelectedTier { get; private set; }

    /// <summary>Notes about the prices (provisional status, validity, VAT).</summary>
    public IReadOnlyList<MenuViewModels.MenuPriceNoteViewModel> PriceNotes { get; private set; } =
        Array.Empty<MenuViewModels.MenuPriceNoteViewModel>();

    /// <summary>
    /// False when the menu data could not be read. The page then shows a
    /// branded unavailable state instead of an empty menu.
    /// </summary>
    public bool DatabaseAvailable { get; private set; } = true;

    /// <summary>
    /// True when the database was reachable but holds no published price
    /// tiers. This is a genuinely empty catalog, not a failure.
    /// </summary>
    public bool CatalogEmpty { get; private set; }

    // -----------------------------------------------------------------
    // Loading
    // -----------------------------------------------------------------

    /// <summary>
    /// Reads the whole public menu for this venue.
    /// </summary>
    /// <param name="tierSelector">
    /// The slug (or the stable key) of the price tier the visitor asked
    /// for. An unknown selector — and a selector of a tier that is not
    /// public — falls back to the first tier.
    /// </param>
    protected async Task LoadMenuAsync(string? tierSelector)
    {
        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogInformation(
                "{Venue} menu: no database context is registered, the menu data is temporarily unavailable.",
                VenueDisplayName);
            return;
        }

        try
        {
            // One scope for the whole request: it knows which price tiers are
            // public and which menu options belong to them.
            var scope = await PublicMenuScope.CreateFilterAsync(_db, VenueKey);

            var tierRows = scope.Tiers;

            if (tierRows.Count == 0)
            {
                CatalogEmpty = true;
                _logger.LogInformation(
                    "{Venue} menu: the database is reachable but holds no published price tiers.",
                    VenueDisplayName);
                return;
            }

            var optionRows = await LoadOptionsAsync(scope);
            await ReportLegacyOptionsAsync();

            Tiers = BuildTiers(tierRows, optionRows);

            PriceNotes = BuildPriceNotes(await LoadPriceNotesAsync());

            SelectedTier = ResolveSelectedTier(Tiers, tierSelector);
        }
        catch (Exception ex) when (IsDataAccessFailure(ex))
        {
            DatabaseAvailable = false;
            CatalogEmpty = false;

            // Only the message is logged — never the exception object — so
            // no connection metadata (host, port, database name) ends up in
            // the log sink.
            _logger.LogWarning(
                "{Venue} menu: the menu data could not be read, the page shows the unavailable state. {Message}",
                VenueDisplayName,
                ex.Message);
        }
    }

    // -----------------------------------------------------------------
    // Queries
    // -----------------------------------------------------------------

    private async Task<List<OptionRow>> LoadOptionsAsync(PublicMenuFilter scope)
    {
        // The options are matched by their price tier, so an option that
        // belongs to no tier (a legacy menu row) can never leak into the
        // tier grid.
        //
        // "?? 0" keeps the collection comparable: an id of 0 never exists,
        // and the null check above already removed those rows.
        var ids = scope.Tiers.Select(t => t.Id).ToList();

        var query = _db!.MenuCombos
            .AsNoTracking()
            .Where(c => c.IsPublished && c.MenuTierId.HasValue && ids.Contains(c.MenuTierId ?? 0));

        // The public scope decides which options of those tiers are public.
        var rows = await scope.ApplyTo(query)
            .OrderBy(c => c.MenuTierId)
            .ThenBy(c => c.OptionSortOrder)
            .ThenBy(c => c.Id)
            .Select(c => new OptionRow(
                c.Id,
                c.MenuTierId!.Value,
                c.OptionCode,
                c.OptionSortOrder,
                c.Name,
                c.Description,
                c.Price,
                c.ComboDishes
                    .Where(link => link.Dish != null && link.Dish.IsPublished)
                    .OrderBy(link => link.SortOrder)
                    .ThenBy(link => link.DishId)
                    .Select(link => new DishRow(
                        link.Dish!.Id,
                        link.Dish.Name,
                        link.Dish.Slug,
                        link.SortOrder,
                        link.Quantity,
                        link.Note))
                    .ToList()))
            .ToListAsync();

        return rows;
    }

    private async Task<List<MenuPriceNote>> LoadPriceNotesAsync()
    {
        return await _db!.MenuPriceNotes
            .AsNoTracking()
            .Where(n => n.IsPublished)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Counts the published menu options that are not attached to a price
    /// tier. They belong to the legacy menu structure and are deliberately
    /// left out of the tier grid; the count is reported so the behaviour is
    /// visible server-side instead of silently dropping data.
    /// </summary>
    private async Task ReportLegacyOptionsAsync()
    {
        var legacyCount = await _db!.MenuCombos
            .AsNoTracking()
            .CountAsync(c => c.IsPublished && !c.MenuTierId.HasValue);

        if (legacyCount > 0)
        {
            _logger.LogInformation(
                "{Venue} menu: {LegacyCount} published menu options have no price tier and are not part of the tier grid.",
                VenueDisplayName,
                legacyCount);
        }
    }

    // -----------------------------------------------------------------
    // Projection
    // -----------------------------------------------------------------

    private IReadOnlyList<MenuViewModels.MenuTierViewModel> BuildTiers(
        IReadOnlyList<MenuTier> tierRows,
        IReadOnlyList<OptionRow> optionRows)
    {
        var optionsByTier = optionRows
            .GroupBy(o => o.TierId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OptionRow>)g.ToList());

        var tiers = new List<MenuViewModels.MenuTierViewModel>(tierRows.Count);

        foreach (var tier in tierRows)
        {
            optionsByTier.TryGetValue(tier.Id, out var tierOptions);
            tierOptions ??= Array.Empty<OptionRow>();

            var options = tierOptions
                .Where(o => !string.IsNullOrWhiteSpace(o.OptionCode))
                .OrderBy(o => o.OptionSortOrder)
                .ThenBy(o => o.Id)
                .Select(o => new MenuViewModels.MenuOptionViewModel(
                    o.Id,
                    o.OptionCode!.Trim(),
                    o.Name,
                    BuildOptionShortName(o.Name, o.OptionCode!.Trim()),
                    o.OptionSortOrder,
                    o.Dishes
                        .OrderBy(d => d.SortOrder)
                        .ThenBy(d => d.DishId)
                        .Select(d => new MenuViewModels.MenuDishViewModel(
                            d.DishId,
                            d.DishName,
                            d.DishSlug,
                            d.Quantity,
                            d.Note))
                        .ToList()))
                .ToList();

            var hasValidPrice = MenuPriceFormatter.IsDisplayable(tier.Price);

            // The price of an option always comes from its tier. A leftover
            // price on the option row itself is ignored — and reported, so
            // the drift is visible server-side instead of silently
            // producing two different prices for the same option.
            foreach (var option in tierOptions)
            {
                if (option.Price is not null && hasValidPrice)
                {
                    _logger.LogWarning(
                        "{Venue} menu: option '{Code}' of tier '{TierKey}' carries its own price {Price}; the tier price is used instead.",
                        VenueDisplayName,
                        option.OptionCode,
                        tier.Key,
                        option.Price);
                }
            }

            tiers.Add(new MenuViewModels.MenuTierViewModel(
                tier.Id,
                tier.Key,
                tier.Name,
                tier.Slug,
                tier.Description,
                tier.Price,
                tier.GuestsPerTable,
                tier.SortOrder,
                hasValidPrice ? MenuPriceFormatter.FormatAmount(tier.Price) : null,
                hasValidPrice,
                tier.VenueScope,
                string.Equals(tier.VenueScope, MenuVenueScopes.BothVenues, StringComparison.Ordinal),
                options));
        }

        return tiers;
    }

    /// <summary>
    /// Builds the short name of a menu option. The stored name repeats the
    /// price tier ("Thực đơn 2.500.000 đồng/bàn - Phương án A"); the page
    /// shows the part after the separator, because the tier is already
    /// visible in the heading of the section.
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

    private static IReadOnlyList<MenuViewModels.MenuPriceNoteViewModel> BuildPriceNotes(
        IReadOnlyList<MenuPriceNote> rows)
    {
        return rows
            .Select(n => new MenuViewModels.MenuPriceNoteViewModel(
                n.Id,
                n.Title,
                n.Content,
                n.ValidUntil,
                n.ValidUntil?.ToString("dd/MM/yyyy"),
                n.VatNote,
                n.SortOrder))
            .ToList();
    }

    private MenuViewModels.MenuTierViewModel? ResolveSelectedTier(
        IReadOnlyList<MenuViewModels.MenuTierViewModel> tiers,
        string? selector)
    {
        if (tiers.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(selector))
        {
            var trimmed = selector.Trim();

            var match = tiers.FirstOrDefault(t => string.Equals(t.Slug, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? tiers.FirstOrDefault(t => string.Equals(t.Key, trimmed, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }

            _logger.LogInformation(
                "{Venue} menu: the requested price tier '{Selector}' is unknown, the first tier is shown instead.",
                VenueDisplayName,
                trimmed);
        }

        return tiers[0];
    }

    /// <summary>
    /// True for the failures that mean "the data could not be read": the
    /// database is unreachable, the schema is missing (the Phase 02A
    /// migration has not been applied) or the query timed out. Programming
    /// errors are deliberately not caught here — they must surface.
    /// </summary>
    private static bool IsDataAccessFailure(Exception exception) =>
        exception is DbException
            or TimeoutException
            or InvalidOperationException;

    // -----------------------------------------------------------------
    // Small row types for the projections
    // -----------------------------------------------------------------

    private sealed record OptionRow(
        int Id,
        int TierId,
        string? OptionCode,
        int OptionSortOrder,
        string Name,
        string? Description,
        decimal? Price,
        List<DishRow> Dishes);

    private sealed record DishRow(
        int DishId,
        string DishName,
        string DishSlug,
        int SortOrder,
        int Quantity,
        string? Note);
}
