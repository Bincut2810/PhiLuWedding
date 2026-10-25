using System.Data.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;
using PhiluWedding.Services;
using MenuViewModels = PhiluWedding.Pages.ThucDon.MenuVenueViewModels;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Shared page model behind both public wedding-menu pages
/// (<c>/thuc-don/philu</c> and <c>/thuc-don/golden-phoenix</c>).
///
/// <para>
/// <b>What is shared and what is not.</b> The eight wedding-menu price tiers
/// and their options (A, B, C) come from one shared catalog, so both venues
/// read exactly the same tier rows and therefore exactly the same prices.
/// The supporting sections (services, beverages, appetizer nuts, promotions,
/// deposit terms) carry a venue scope in the data model and are filtered by
/// it here — a row scoped to one venue is never shown on the other venue's
/// page.
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
    /// <summary>
    /// Longest inheritance chain walked when the cumulative promotion
    /// benefits are rebuilt. The catalog has eight tiers; the limit only
    /// protects against a cycle introduced by bad data.
    /// </summary>
    private const int MaxPromotionChainLength = 64;

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

    /// <summary>The eight shared price tiers, in their configured order.</summary>
    public IReadOnlyList<MenuViewModels.MenuTierViewModel> Tiers { get; private set; } =
        Array.Empty<MenuViewModels.MenuTierViewModel>();

    /// <summary>The tier the visitor is currently looking at.</summary>
    public MenuViewModels.MenuTierViewModel? SelectedTier { get; private set; }

    /// <summary>Additional services and fees in scope for this venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuServiceViewModel> Services { get; private set; } =
        Array.Empty<MenuViewModels.MenuServiceViewModel>();

    /// <summary>Beverages in scope for this venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuBeverageViewModel> Beverages { get; private set; } =
        Array.Empty<MenuViewModels.MenuBeverageViewModel>();

    /// <summary>Appetizer nuts in scope for this venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuAppetizerNutViewModel> Nuts { get; private set; } =
        Array.Empty<MenuViewModels.MenuAppetizerNutViewModel>();

    /// <summary>Promotion tiers in scope for this venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuPromotionViewModel> Promotions { get; private set; } =
        Array.Empty<MenuViewModels.MenuPromotionViewModel>();

    /// <summary>The shared conditions of the promotion tiers.</summary>
    public IReadOnlyList<MenuViewModels.MenuPromotionTermViewModel> PromotionTerms { get; private set; } =
        Array.Empty<MenuViewModels.MenuPromotionTermViewModel>();

    /// <summary>The deposit milestones of the halls of this venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuDepositHallViewModel> DepositHalls { get; private set; } =
        Array.Empty<MenuViewModels.MenuDepositHallViewModel>();

    /// <summary>Deposit milestones that apply to the whole venue.</summary>
    public IReadOnlyList<MenuViewModels.MenuDepositStageViewModel> VenueWideDepositStages { get; private set; } =
        Array.Empty<MenuViewModels.MenuDepositStageViewModel>();

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

    /// <summary>True when the venue page shows the deposit terms.</summary>
    public bool HasDepositTerms =>
        DepositHalls.Count > 0 || VenueWideDepositStages.Count > 0;

    /// <summary>True when the venue page shows the promotion section.</summary>
    public bool HasPromotions => Promotions.Count > 0;

    /// <summary>True when the venue page shows the beverage section.</summary>
    public bool HasBeverages => Beverages.Count > 0;

    /// <summary>True when the venue page shows the appetizer nut section.</summary>
    public bool HasNuts => Nuts.Count > 0;

    /// <summary>True when the venue page shows the services and fees section.</summary>
    public bool HasServices => Services.Count > 0;

    /// <summary>
    /// The table count that separates the two source beverage prices. All
    /// beverages of the catalog share it.
    /// </summary>
    public int BeverageThresholdTableCount =>
        Beverages.Count > 0 ? Beverages[0].ThresholdTableCount : 0;

    /// <summary>The wording of the beverage threshold, exactly as the source states it.</summary>
    public string? BeverageThresholdNote =>
        Beverages.Count > 0 ? Beverages[0].ThresholdNote : null;

    /// <summary>
    /// True when the venue has no supporting section in scope, so the page
    /// explains that those items are published for the other venue only.
    /// </summary>
    public bool HasNoSupportingSections =>
        !HasServices && !HasBeverages && !HasNuts && !HasPromotions && !HasDepositTerms;

    // -----------------------------------------------------------------
    // Loading
    // -----------------------------------------------------------------

    /// <summary>
    /// Reads the whole public menu for this venue.
    /// </summary>
    /// <param name="tierSelector">
    /// The slug (or the stable key) of the price tier the visitor asked
    /// for. An unknown selector falls back to the first tier.
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
            var tierRows = await LoadTiersAsync();

            if (tierRows.Count == 0)
            {
                CatalogEmpty = true;
                _logger.LogInformation(
                    "{Venue} menu: the database is reachable but holds no published price tiers.",
                    VenueDisplayName);
                return;
            }

            var tierIds = tierRows.Select(t => t.Id).ToList();
            var optionRows = await LoadOptionsAsync(tierIds);
            await ReportLegacyOptionsAsync();

            Tiers = BuildTiers(tierRows, optionRows);

            var services = await LoadServicesAsync();
            var beverages = await LoadBeveragesAsync();
            var nuts = await LoadNutsAsync();
            var promotions = await LoadPromotionsAsync();
            var depositTerms = await LoadDepositTermsAsync();
            var priceNotes = await LoadPriceNotesAsync();

            Services = BuildServices(services);
            Beverages = BuildBeverages(beverages);
            Nuts = BuildNuts(nuts);
            Promotions = BuildPromotions(promotions);
            PriceNotes = BuildPriceNotes(priceNotes);

            // The promotion conditions belong to the promotion tiers. When no
            // promotion is in scope for this venue, the conditions are not
            // shown either.
            PromotionTerms = promotions.Count > 0
                ? BuildPromotionTerms(await LoadPromotionTermsAsync())
                : Array.Empty<MenuViewModels.MenuPromotionTermViewModel>();

            ApplyDepositTerms(depositTerms);

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

    private async Task<List<MenuTier>> LoadTiersAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuTiers
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .Where(t => t.VenueScope == MenuVenueScopes.BothVenues || t.VenueScope == venueKey)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    private async Task<List<OptionRow>> LoadOptionsAsync(IReadOnlyCollection<int> tierIds)
    {
        // The options are matched by their price tier, so an option that
        // belongs to no tier (a legacy menu row) can never leak into the
        // tier grid.
        //
        // "?? 0" keeps the collection comparable: an id of 0 never exists,
        // and the null check above already removed those rows.
        var ids = tierIds.ToList();

        var rows = await _db!.MenuCombos
            .AsNoTracking()
            .Where(c => c.IsPublished && c.MenuTierId.HasValue && ids.Contains(c.MenuTierId ?? 0))
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

    private async Task<List<MenuService>> LoadServicesAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuServices
            .AsNoTracking()
            .Where(s => s.IsPublished)
            .Where(s => s.VenueScope == MenuVenueScopes.BothVenues || s.VenueScope == venueKey)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();
    }

    private async Task<List<MenuBeverage>> LoadBeveragesAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuBeverages
            .AsNoTracking()
            .Where(b => b.IsPublished)
            .Where(b => b.VenueScope == MenuVenueScopes.BothVenues || b.VenueScope == venueKey)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .ToListAsync();
    }

    private async Task<List<MenuAppetizerNut>> LoadNutsAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuAppetizerNuts
            .AsNoTracking()
            .Where(n => n.IsPublished)
            .Where(n => n.VenueScope == MenuVenueScopes.BothVenues || n.VenueScope == venueKey)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Id)
            .ToListAsync();
    }

    private async Task<List<PromotionRow>> LoadPromotionsAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuPromotions
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .Where(p => p.VenueScope == MenuVenueScopes.BothVenues || p.VenueScope == venueKey)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Select(p => new PromotionRow(
                p.Id,
                p.Name,
                p.RangeLabel,
                p.MinTableCount,
                p.MaxTableCount,
                p.BasePromotionId,
                p.IncludesPreviousTier,
                p.SortOrder,
                p.Benefits
                    .OrderBy(b => b.SortOrder)
                    .ThenBy(b => b.Id)
                    .Select(b => b.Text)
                    .ToList()))
            .ToListAsync();
    }

    private async Task<List<MenuPromotionTerm>> LoadPromotionTermsAsync()
    {
        return await _db!.MenuPromotionTerms
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    private async Task<List<MenuDepositTerm>> LoadDepositTermsAsync()
    {
        var venueKey = VenueKey;

        return await _db!.MenuDepositTerms
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .Where(t => t.VenueScope == MenuVenueScopes.BothVenues || t.VenueScope == venueKey)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();
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
    /// price tier ("Thực đơn 2.200.000 đồng/bàn - Phương án A"); the page
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

    private static IReadOnlyList<MenuViewModels.MenuServiceViewModel> BuildServices(
        IReadOnlyList<MenuService> rows)
    {
        return rows
            .Select(s =>
            {
                var price = MenuPriceFormatter.FormatServicePrice(s);

                return new MenuViewModels.MenuServiceViewModel(
                    s.Id,
                    s.Name,
                    s.PriceType,
                    price,
                    price is null ? MenuPriceFormatter.DescribeUnpriced(s.PriceType, s.Note) : null,
                    s.Unit,
                    s.Note,
                    s.SortOrder);
            })
            .ToList();
    }

    private static IReadOnlyList<MenuViewModels.MenuBeverageViewModel> BuildBeverages(
        IReadOnlyList<MenuBeverage> rows)
    {
        return rows
            .Select(b => new MenuViewModels.MenuBeverageViewModel(
                b.Id,
                b.Name,
                MenuPriceFormatter.FormatAmountWithUnit(b.PriceBelowThreshold, b.Unit),
                MenuPriceFormatter.FormatAmountWithUnit(b.PriceAboveThreshold, b.Unit),
                b.ThresholdTableCount,
                b.ThresholdNote,
                b.Unit,
                b.Note,
                b.SortOrder))
            .ToList();
    }

    private static IReadOnlyList<MenuViewModels.MenuAppetizerNutViewModel> BuildNuts(
        IReadOnlyList<MenuAppetizerNut> rows)
    {
        return rows
            .Select(n =>
            {
                var price = MenuPriceFormatter.FormatAppetizerNutPrice(n);

                return new MenuViewModels.MenuAppetizerNutViewModel(
                    n.Id,
                    n.Name,
                    n.Description,
                    price,
                    price is null ? MenuPriceFormatter.DescribeUnpriced(n.PriceType, n.Note) : null,
                    n.Unit,
                    n.Note,
                    n.SortOrder);
            })
            .ToList();
    }

    private static IReadOnlyList<MenuViewModels.MenuPromotionViewModel> BuildPromotions(
        IReadOnlyList<PromotionRow> rows)
    {
        var byId = rows.ToDictionary(r => r.Id);

        var result = new List<MenuViewModels.MenuPromotionViewModel>(rows.Count);

        foreach (var row in rows)
        {
            var chain = new List<PromotionRow>();
            var current = row;
            var guard = 0;

            while (current is not null && guard++ < MaxPromotionChainLength)
            {
                chain.Add(current);

                if (!current.BasePromotionId.HasValue)
                {
                    break;
                }

                current = byId.TryGetValue(current.BasePromotionId.Value, out var parent) ? parent : null;
            }

            chain.Reverse();

            var cumulative = chain
                .SelectMany(c => c.Benefits)
                .ToList();

            var baseRangeLabel = row.BasePromotionId.HasValue
                && byId.TryGetValue(row.BasePromotionId.Value, out var baseRow)
                    ? baseRow.RangeLabel
                    : null;

            result.Add(new MenuViewModels.MenuPromotionViewModel(
                row.Id,
                row.Name,
                row.RangeLabel,
                row.MinTableCount,
                row.MaxTableCount,
                row.IncludesPreviousTier,
                baseRangeLabel,
                row.Benefits,
                cumulative,
                row.SortOrder));
        }

        return result;
    }

    private void ApplyDepositTerms(IReadOnlyList<MenuDepositTerm> rows)
    {
        var halls = new List<MenuViewModels.MenuDepositHallViewModel>();
        var venueWide = new List<MenuViewModels.MenuDepositStageViewModel>();

        // Group by the stable hall slug, never by a database id, so the
        // terms still resolve before the hall rows exist.
        var byHall = rows
            .Where(t => !string.IsNullOrWhiteSpace(t.HallSlug))
            .GroupBy(t => t.HallSlug!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in byHall)
        {
            var first = group.FirstOrDefault(t => t.Stage == DepositStage.First);
            var second = group.FirstOrDefault(t => t.Stage == DepositStage.Second);

            halls.Add(new MenuViewModels.MenuDepositHallViewModel(
                group.Key,
                group.Select(t => t.HallName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? group.Key,
                group.Select(t => t.WeddingHallId).FirstOrDefault(id => id.HasValue),
                first is null ? null : BuildStage(first),
                second is null ? null : BuildStage(second)));
        }

        venueWide.AddRange(rows
            .Where(t => string.IsNullOrWhiteSpace(t.HallSlug))
            .Select(BuildStage));

        DepositHalls = halls;
        VenueWideDepositStages = venueWide;
    }

    private static MenuViewModels.MenuDepositStageViewModel BuildStage(MenuDepositTerm term)
    {
        return new MenuViewModels.MenuDepositStageViewModel(
            term.Stage switch
            {
                DepositStage.First => "Đặt cọc lần 1",
                DepositStage.Second => "Đặt cọc lần 2",
                DepositStage.Final => "Thanh toán phần còn lại",
                _ => term.Stage.ToString()
            },
            MenuPriceFormatter.FormatAmount(term.Amount),
            term.Description);
    }

    private static IReadOnlyList<MenuViewModels.MenuPromotionTermViewModel> BuildPromotionTerms(
        IReadOnlyList<MenuPromotionTerm> rows)
    {
        return rows
            .Select(t => new MenuViewModels.MenuPromotionTermViewModel(t.Id, t.Content, t.SortOrder))
            .ToList();
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

    private sealed record PromotionRow(
        int Id,
        string Name,
        string RangeLabel,
        int? MinTableCount,
        int? MaxTableCount,
        int? BasePromotionId,
        bool IncludesPreviousTier,
        int SortOrder,
        List<string> Benefits);
}
