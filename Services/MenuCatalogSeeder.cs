using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;

namespace PhiluWedding.Services;

/// <summary>
/// Seeds the fixed wedding-menu catalog from the version-controlled file
/// <c>Seed/menu-catalog.json</c>.
///
/// <para>
/// <b>Safety contract</b>
/// <list type="bullet">
///   <item><description>
///     Nothing is ever deleted. The seeder only inserts rows that are missing.
///   </description></item>
///   <item><description>
///     Prices are written once, when the price row is created. A price that an
///     administrator has edited afterwards is never overwritten.
///   </description></item>
///   <item><description>
///     Publication flags and notes of existing rows are never reset.
///   </description></item>
///   <item><description>
///     Every record is matched on its own stable key, so one existing row
///     never blocks the rest of the catalog from being seeded.
///   </description></item>
/// </list>
/// </para>
///
/// <para>
/// The only three fields the seeder is allowed to correct on an existing row
/// are structural: the price-tier link of a menu option that has none, the
/// sort order of a dish link, and the inherited flag of a promotion tier
/// that inherits from no other tier. All three are fixed content that must
/// match the catalog exactly; none of them is an administrator-maintained
/// value.
/// </para>
/// </summary>
public static class MenuCatalogSeeder
{
    private const string CatalogResourceFileName = "menu-catalog.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads the seed catalog from the embedded resource that ships with the
    /// application.
    /// </summary>
    public static MenuCatalogDocument LoadCatalog()
    {
        var assembly = typeof(MenuCatalogSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(CatalogResourceFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{CatalogResourceFileName}' was not found in {assembly.GetName().Name}.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' could not be opened.");

        // StreamReader with BOM detection strips a possible byte order mark so
        // the JSON parser always sees plain UTF-8.
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var json = reader.ReadToEnd();

        return JsonSerializer.Deserialize<MenuCatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("The menu catalog seed file is empty.");
    }

    /// <summary>Loads the embedded catalog and seeds it into the database.</summary>
    public static Task<MenuSeedReport> SeedAsync(
        PhiluWeddingDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default) =>
        SeedAsync(db, logger, LoadCatalog(), cancellationToken);

    /// <summary>Seeds the given catalog into the database.</summary>
    public static async Task<MenuSeedReport> SeedAsync(
        PhiluWeddingDbContext db,
        ILogger logger,
        MenuCatalogDocument catalog,
        CancellationToken cancellationToken = default)
    {
        var report = new MenuSeedReport();
        var now = DateTime.UtcNow;

        var menuSet = await EnsureMenuSetAsync(db, catalog, now, report, cancellationToken);
        if (menuSet is null)
        {
            report.Errors.Add("The wedding menu set could not be resolved, so no menu content was seeded.");
            return report;
        }

        await SeedTiersAndOptionsAsync(db, catalog, menuSet, now, report, cancellationToken);
        await SeedServicesAsync(db, catalog, now, report, cancellationToken);
        await SeedBeveragesAsync(db, catalog, now, report, cancellationToken);
        await SeedNutsAsync(db, catalog, now, report, cancellationToken);
        await SeedPromotionsAsync(db, catalog, now, report, cancellationToken);
        await SeedPromotionTermsAsync(db, catalog, now, report, cancellationToken);
        await SeedDepositTermsAsync(db, catalog, now, report, cancellationToken);
        await SeedPriceNotesAsync(db, catalog, now, report, cancellationToken);

        logger.LogInformation("MenuCatalogSeeder: finished. {Summary}", report.Summary());
        foreach (var conflict in report.Conflicts)
        {
            logger.LogWarning("MenuCatalogSeeder conflict: {Conflict}", conflict);
        }

        foreach (var error in report.Errors)
        {
            logger.LogWarning("MenuCatalogSeeder error: {Error}", error);
        }

        return report;
    }

    // ---------------------------------------------------------------------
    // Menu set
    // ---------------------------------------------------------------------

    private static async Task<MenuSet?> EnsureMenuSetAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var definition = catalog.MenuSet;
        if (definition is null || string.IsNullOrWhiteSpace(definition.Slug))
        {
            report.Errors.Add("The seed file has no 'menuSet' section.");
            return null;
        }

        var existing = await db.MenuSets
            .FirstOrDefaultAsync(m => m.Slug == definition.Slug, ct);

        if (existing is not null)
        {
            report.CountKept("menuSets");
            if (!string.Equals(existing.Name, definition.Name, StringComparison.Ordinal))
            {
                report.Conflicts.Add(
                    $"Menu set '{definition.Slug}' already exists with name '{existing.Name}'; the seed file says '{definition.Name}'. The existing row was kept.");
            }

            return existing;
        }

        var menuSet = new MenuSet
        {
            Name = definition.Name,
            Slug = definition.Slug,
            ShortDescription = definition.ShortDescription,
            Description = definition.Description,
            VenueScope = ResolveVenueScope(definition.VenueScope, report, $"menu set '{definition.Slug}'"),
            SortOrder = definition.SortOrder,
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MenuSets.Add(menuSet);
        await db.SaveChangesAsync(ct);
        report.CountCreated("menuSets");
        return menuSet;
    }

    // ---------------------------------------------------------------------
    // Tiers, options and dishes
    // ---------------------------------------------------------------------

    private static async Task SeedTiersAndOptionsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        MenuSet menuSet,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        // --- price tiers -------------------------------------------------
        var tiers = new Dictionary<string, MenuTier>(StringComparer.Ordinal);

        foreach (var definition in catalog.Tiers)
        {
            var tier = await db.MenuTiers.FirstOrDefaultAsync(t => t.Key == definition.Key, ct);
            if (tier is not null)
            {
                report.CountKept("tiers");
                if (tier.Price != definition.Price)
                {
                    report.Conflicts.Add(
                        $"Price tier '{definition.Key}' already exists with price {tier.Price}; the seed file says {definition.Price}. The stored price was kept.");
                }

                tiers[definition.Key] = tier;
                continue;
            }

            tier = new MenuTier
            {
                Key = definition.Key,
                Name = definition.Name,
                Slug = definition.Slug,
                Description = definition.Description,
                Price = definition.Price,
                GuestsPerTable = definition.GuestsPerTable,
                SortOrder = definition.SortOrder,
                VenueScope = ResolveVenueScope(definition.VenueScope, report, $"price tier '{definition.Key}'"),
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.MenuTiers.Add(tier);
            tiers[definition.Key] = tier;
            report.CountCreated("tiers");
        }

        await db.SaveChangesAsync(ct);

        // --- menu options ------------------------------------------------
        foreach (var tierDefinition in catalog.Tiers)
        {
            if (!tiers.TryGetValue(tierDefinition.Key, out var tier))
            {
                continue;
            }

            foreach (var optionDefinition in tierDefinition.Options)
            {
                var code = optionDefinition.Code?.Trim().ToUpperInvariant() ?? string.Empty;
                if (code.Length == 0)
                {
                    report.Errors.Add($"Option without a code in tier '{tierDefinition.Key}' was skipped.");
                    continue;
                }

                var option = await db.MenuCombos
                    .FirstOrDefaultAsync(c => c.MenuTierId == tier.Id && c.OptionCode == code, ct);

                if (option is null)
                {
                    // The option may exist but have no price-tier link yet
                    // (a partially seeded catalog). It is linked to the tier
                    // instead of creating a second option with the same code.
                    var unlinked = await db.MenuCombos
                        .FirstOrDefaultAsync(c => c.MenuSetId == menuSet.Id && c.OptionCode == code && c.MenuTierId == null, ct);

                    if (unlinked is not null)
                    {
                        unlinked.MenuTierId = tier.Id;
                        unlinked.UpdatedAt = now;
                        report.Conflicts.Add(
                            $"Menu option '{code}' of tier '{tierDefinition.Key}' existed without a price-tier link; the link was filled in.");
                        report.CountKept("options");
                        continue;
                    }
                }

                if (option is not null)
                {
                    report.CountKept("options");
                    continue;
                }

                option = new MenuCombo
                {
                    MenuSetId = menuSet.Id,
                    MenuTierId = tier.Id,
                    OptionCode = code,
                    OptionSortOrder = optionDefinition.SortOrder,
                    Name = $"{tierDefinition.Name} - {optionDefinition.Name}",
                    Slug = $"{tierDefinition.Slug}-{code.ToLowerInvariant()}",
                    Description = optionDefinition.Description,
                    // The price lives on the tier and is shared by every option
                    // of that tier, so the option itself carries no price.
                    Price = null,
                    GuestCount = tierDefinition.GuestsPerTable,
                    SortOrder = tierDefinition.SortOrder * 100 + optionDefinition.SortOrder,
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                db.MenuCombos.Add(option);
                report.CountCreated("options");
            }
        }

        await db.SaveChangesAsync(ct);

        // --- dishes ------------------------------------------------------
        // The global dish catalog holds every distinct dish exactly once. The
        // same dish may appear in many options, including several options of
        // the same price tier.
        var existingDishes = await db.Dishes.ToListAsync(ct);
        var dishesByKey = new Dictionary<string, Dish>(StringComparer.Ordinal);
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dish in existingDishes)
        {
            var key = MenuText.NormalizeKey(dish.Name);
            if (!dishesByKey.ContainsKey(key))
            {
                dishesByKey[key] = dish;
            }

            usedSlugs.Add(dish.Slug);
        }

        // Order in which the dishes first appear in the catalog. It is only
        // used to give the dish rows a stable display order.
        var nextDishOrder = 1;

        foreach (var tierDefinition in catalog.Tiers)
        {
            foreach (var optionDefinition in tierDefinition.Options)
            {
                foreach (var dishName in optionDefinition.Dishes)
                {
                    var key = MenuText.NormalizeKey(dishName);
                    if (key.Length == 0 || dishesByKey.ContainsKey(key))
                    {
                        continue;
                    }

                    var slug = BuildUniqueSlug(dishName, usedSlugs);
                    var dish = new Dish
                    {
                        Name = dishName,
                        Slug = slug,
                        Description = null,
                        Ingredients = null,
                        Price = null,
                        SortOrder = nextDishOrder * 10,
                        IsPublished = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    db.Dishes.Add(dish);
                    dishesByKey[key] = dish;
                    usedSlugs.Add(slug);
                    nextDishOrder++;
                    report.CountCreated("dishes");
                }
            }
        }

        await db.SaveChangesAsync(ct);

        // --- dish links --------------------------------------------------
        // The options are matched by price tier AND option code: every tier
        // has its own option "A" and "B".
        var seededOptions = await db.MenuCombos
            .Where(c => c.MenuSetId == menuSet.Id && c.OptionCode != null)
            .ToListAsync(ct);

        var optionIds = seededOptions.Select(o => o.Id).ToList();
        var existingLinks = await db.MenuComboDishes
            .Where(l => optionIds.Contains(l.MenuComboId))
            .ToListAsync(ct);

        foreach (var tierDefinition in catalog.Tiers)
        {
            if (!tiers.TryGetValue(tierDefinition.Key, out var tier))
            {
                continue;
            }

            foreach (var optionDefinition in tierDefinition.Options)
            {
                var code = optionDefinition.Code?.Trim().ToUpperInvariant() ?? string.Empty;
                var option = seededOptions.FirstOrDefault(o => o.MenuTierId == tier.Id && o.OptionCode == code);
                if (option is null)
                {
                    report.Errors.Add(
                        $"Menu option '{code}' of tier '{tierDefinition.Key}' could not be resolved.");
                    continue;
                }

                var sortOrder = 0;
                foreach (var dishName in optionDefinition.Dishes)
                {
                    sortOrder++;
                    var key = MenuText.NormalizeKey(dishName);
                    if (!dishesByKey.TryGetValue(key, out var dish))
                    {
                        report.Errors.Add(
                            $"Dish '{dishName}' of option '{code}' could not be resolved.");
                        continue;
                    }

                    var link = existingLinks.FirstOrDefault(l => l.MenuComboId == option.Id && l.DishId == dish.Id);

                    if (link is null)
                    {
                        var created = new MenuComboDish
                        {
                            MenuComboId = option.Id,
                            DishId = dish.Id,
                            SortOrder = sortOrder,
                            Quantity = 1,
                            Note = null
                        };

                        db.MenuComboDishes.Add(created);
                        existingLinks.Add(created);
                        report.CountCreated("dishLinks");
                        continue;
                    }

                    report.CountKept("dishLinks");

                    // Dish order is fixed catalog content, so a mismatch is
                    // corrected. Prices, publication flags and notes are not
                    // touched here.
                    if (link.SortOrder != sortOrder)
                    {
                        report.Conflicts.Add(
                            $"Dish link '{dishName}' in option '{code}' was stored with order {link.SortOrder}; the catalog says {sortOrder}. The stored order was corrected.");
                        link.SortOrder = sortOrder;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Services
    // ---------------------------------------------------------------------

    private static async Task SeedServicesAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuServices.Select(s => s.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.Services)
        {
            var existing = await db.MenuServices.FirstOrDefaultAsync(s => s.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("services");
                ReportPriceConflict(report, $"Service '{definition.Key}'", existing.PriceFrom, existing.PriceTo, definition.PriceFrom, definition.PriceTo);
                continue;
            }

            db.MenuServices.Add(new MenuService
            {
                Key = definition.Key,
                Name = definition.Name,
                Slug = BuildUniqueSlug(definition.Name, slugs),
                PriceType = ResolvePriceType(definition.PriceType, report, $"service '{definition.Key}'"),
                PriceFrom = definition.PriceFrom,
                PriceTo = definition.PriceTo,
                Unit = definition.Unit,
                Note = definition.Note,
                SortOrder = definition.SortOrder,
                VenueScope = MenuVenueScopes.PhiLu,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Name));
            report.CountCreated("services");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Beverages
    // ---------------------------------------------------------------------

    private static async Task SeedBeveragesAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuBeverages.Select(b => b.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.Beverages)
        {
            var existing = await db.MenuBeverages.FirstOrDefaultAsync(b => b.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("beverages");
                ReportPriceConflict(
                    report,
                    $"Beverage '{definition.Key}'",
                    existing.PriceBelowThreshold,
                    existing.PriceAboveThreshold,
                    definition.PriceBelowThreshold,
                    definition.PriceAboveThreshold);
                continue;
            }

            db.MenuBeverages.Add(new MenuBeverage
            {
                Key = definition.Key,
                Name = definition.Name,
                Slug = BuildUniqueSlug(definition.Name, slugs),
                PriceBelowThreshold = definition.PriceBelowThreshold,
                PriceAboveThreshold = definition.PriceAboveThreshold,
                ThresholdTableCount = BeverageThresholdTableCount,
                ThresholdNote = BeverageThresholdNote,
                Unit = definition.Unit,
                Note = definition.Note,
                SortOrder = definition.SortOrder,
                VenueScope = MenuVenueScopes.PhiLu,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Name));
            report.CountCreated("beverages");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Appetizer nuts
    // ---------------------------------------------------------------------

    private static async Task SeedNutsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuAppetizerNuts.Select(n => n.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.Nuts)
        {
            var existing = await db.MenuAppetizerNuts.FirstOrDefaultAsync(n => n.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("nuts");
                ReportPriceConflict(report, $"Appetizer nut '{definition.Key}'", existing.Price, null, definition.Price, null);
                continue;
            }

            db.MenuAppetizerNuts.Add(new MenuAppetizerNut
            {
                Key = definition.Key,
                Name = definition.Name,
                Slug = BuildUniqueSlug(definition.Name, slugs),
                PriceType = ResolvePriceType(definition.PriceType, report, $"appetizer nut '{definition.Key}'"),
                Price = definition.Price,
                Unit = definition.Unit,
                Note = definition.Note,
                SortOrder = definition.SortOrder,
                VenueScope = MenuVenueScopes.PhiLu,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Name));
            report.CountCreated("nuts");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Promotions
    // ---------------------------------------------------------------------

    private static async Task SeedPromotionsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuPromotions.Select(p => p.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.Promotions)
        {
            var existing = await db.MenuPromotions.FirstOrDefaultAsync(p => p.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("promotions");
                continue;
            }

            db.MenuPromotions.Add(new MenuPromotion
            {
                Key = definition.Key,
                Name = definition.Name,
                Slug = BuildUniqueSlug(definition.Name, slugs),
                RangeLabel = definition.RangeLabel,
                MinTableCount = definition.MinTableCount,
                MaxTableCount = definition.MaxTableCount,
                SortOrder = definition.SortOrder,
                VenueScope = MenuVenueScopes.PhiLu,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Name));
            report.CountCreated("promotions");
        }

        await db.SaveChangesAsync(ct);

        // Resolve the inheritance chain now that every tier has an id.
        foreach (var definition in catalog.Promotions)
        {
            var promotion = await db.MenuPromotions.FirstOrDefaultAsync(p => p.Key == definition.Key, ct);
            if (promotion is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.BaseKey))
            {
                // The first tier inherits from no other tier. The flag is
                // structural, so a stale value is corrected here.
                if (promotion.BasePromotionId is null && promotion.IncludesPreviousTier)
                {
                    promotion.IncludesPreviousTier = false;
                    promotion.UpdatedAt = now;
                    report.Conflicts.Add(
                        $"Promotion '{definition.Key}' inherits from no other tier; the inherited flag was cleared.");
                }

                continue;
            }

            var basePromotion = await db.MenuPromotions
                .FirstOrDefaultAsync(p => p.Key == definition.BaseKey, ct);

            if (basePromotion is null)
            {
                report.Errors.Add(
                    $"Promotion '{definition.Key}' inherits from '{definition.BaseKey}', which does not exist.");
            }
            else if (promotion.BasePromotionId != basePromotion.Id)
            {
                promotion.BasePromotionId = basePromotion.Id;
                promotion.IncludesPreviousTier = true;
                promotion.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(ct);

        // Benefits: one row per benefit, in the order of the source sheet.
        foreach (var definition in catalog.Promotions)
        {
            var promotion = await db.MenuPromotions.FirstOrDefaultAsync(p => p.Key == definition.Key, ct);
            if (promotion is null)
            {
                continue;
            }

            var existingBenefits = await db.MenuPromotionBenefits
                .Where(b => b.MenuPromotionId == promotion.Id)
                .ToListAsync(ct);

            var sortOrder = 0;
            foreach (var text in definition.Benefits)
            {
                sortOrder++;

                var existing = existingBenefits.FirstOrDefault(b => b.SortOrder == sortOrder);
                if (existing is not null)
                {
                    report.CountKept("promotionBenefits");
                    if (!string.Equals(existing.Text, text, StringComparison.Ordinal))
                    {
                        report.Conflicts.Add(
                            $"Promotion '{definition.Key}' benefit {sortOrder} already exists with different text; the existing text was kept.");
                    }

                    continue;
                }

                db.MenuPromotionBenefits.Add(new MenuPromotionBenefit
                {
                    MenuPromotionId = promotion.Id,
                    SortOrder = sortOrder,
                    Text = text
                });

                report.CountCreated("promotionBenefits");
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Promotion terms
    // ---------------------------------------------------------------------

    private static async Task SeedPromotionTermsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuPromotionTerms.Select(t => t.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.PromotionTerms)
        {
            var existing = await db.MenuPromotionTerms.FirstOrDefaultAsync(t => t.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("promotionTerms");
                if (!string.Equals(existing.Content, definition.Content, StringComparison.Ordinal))
                {
                    report.Conflicts.Add(
                        $"Promotion term '{definition.Key}' already exists with different text; the existing text was kept.");
                }

                continue;
            }

            db.MenuPromotionTerms.Add(new MenuPromotionTerm
            {
                Key = definition.Key,
                Slug = BuildUniqueSlug(definition.Key, slugs),
                Content = definition.Content,
                SortOrder = definition.SortOrder,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Key));
            report.CountCreated("promotionTerms");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Deposit terms
    // ---------------------------------------------------------------------

    private static async Task SeedDepositTermsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuDepositTerms.Select(t => t.Key).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.DepositTerms)
        {
            var existing = await db.MenuDepositTerms.FirstOrDefaultAsync(t => t.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("depositTerms");
                ReportPriceConflict(report, $"Deposit term '{definition.Key}'", existing.Amount, null, definition.Amount, null);

                // The hall link is structural: when the hall row is created
                // after the term was seeded, the link is filled in here. The
                // amount is never touched.
                if (existing.WeddingHallId is null && !string.IsNullOrWhiteSpace(definition.HallSlug))
                {
                    var linkedHall = await db.WeddingHalls
                        .FirstOrDefaultAsync(h => h.VenueKey == definition.VenueScope && h.Slug == definition.HallSlug, ct);

                    if (linkedHall is not null)
                    {
                        existing.WeddingHallId = linkedHall.Id;
                        existing.UpdatedAt = now;
                        report.Conflicts.Add(
                            $"Deposit term '{definition.Key}' was linked to hall '{definition.HallSlug}'.");
                    }
                }

                continue;
            }

            if (!Enum.TryParse<DepositStage>(definition.Stage, ignoreCase: true, out var stage))
            {
                report.Errors.Add(
                    $"Deposit term '{definition.Key}' has an unknown stage '{definition.Stage}'.");
                continue;
            }

            int? hallId = null;
            if (!string.IsNullOrWhiteSpace(definition.HallSlug))
            {
                // Halls are looked up by their stable slug, never by a
                // hard-coded id, so the term still works when the hall rows
                // are created later.
                var hall = await db.WeddingHalls
                    .FirstOrDefaultAsync(h => h.VenueKey == definition.VenueScope && h.Slug == definition.HallSlug, ct);

                if (hall is null)
                {
                    report.Conflicts.Add(
                        $"Deposit term '{definition.Key}' refers to hall '{definition.HallSlug}', which is not in the database yet. The term was stored without a hall link.");
                }
                else
                {
                    hallId = hall.Id;
                }
            }

            db.MenuDepositTerms.Add(new MenuDepositTerm
            {
                Key = definition.Key,
                VenueScope = ResolveVenueScope(definition.VenueScope, report, $"deposit term '{definition.Key}'"),
                HallSlug = definition.HallSlug,
                HallName = definition.HallName,
                WeddingHallId = hallId,
                Stage = stage,
                Amount = definition.Amount,
                Description = definition.Description,
                SortOrder = definition.SortOrder,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(definition.Key);
            report.CountCreated("depositTerms");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Price notes
    // ---------------------------------------------------------------------

    private static async Task SeedPriceNotesAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        DateTime now,
        MenuSeedReport report,
        CancellationToken ct)
    {
        var slugs = new HashSet<string>(
            await db.MenuPriceNotes.Select(n => n.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in catalog.PriceNotes)
        {
            var existing = await db.MenuPriceNotes.FirstOrDefaultAsync(n => n.Key == definition.Key, ct);
            if (existing is not null)
            {
                report.CountKept("priceNotes");
                if (!string.Equals(existing.Content, definition.Content, StringComparison.Ordinal))
                {
                    report.Conflicts.Add(
                        $"Price note '{definition.Key}' already exists with different text; the existing text was kept.");
                }

                continue;
            }

            db.MenuPriceNotes.Add(new MenuPriceNote
            {
                Key = definition.Key,
                Title = definition.Title,
                Slug = BuildUniqueSlug(definition.Title, slugs),
                Content = definition.Content,
                ValidUntil = definition.ValidUntil,
                VatNote = definition.VatNote,
                Note = definition.Note,
                SortOrder = definition.SortOrder,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            slugs.Add(MenuText.Slugify(definition.Title));
            report.CountCreated("priceNotes");
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Table count that separates the two beverage prices. The source says
    /// "dưới 25 bàn" and "trên 25 bàn" and says nothing about exactly 25.
    /// </summary>
    private const int BeverageThresholdTableCount = 25;

    private const string BeverageThresholdNote =
        "Giá theo bảng giá gốc: dưới 25 bàn và trên 25 bàn. Bảng giá gốc không nêu mức giá khi đúng 25 bàn.";

    /// <summary>
    /// Builds a slug that is not used by another row yet.
    /// </summary>
    private static string BuildUniqueSlug(string source, HashSet<string> usedSlugs)
    {
        var baseSlug = MenuText.Slugify(source);
        if (baseSlug.Length == 0)
        {
            baseSlug = "menu-item";
        }

        if (!usedSlugs.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!usedSlugs.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Validates a venue scope and falls back to the Phì Lũ scope when the
    /// seed file contains something unexpected, so a typo never widens the
    /// applicability of a row.
    /// </summary>
    private static string ResolveVenueScope(string? scope, MenuSeedReport report, string subject)
    {
        if (MenuVenueScopes.IsKnown(scope))
        {
            return scope!;
        }

        report.Errors.Add(
            $"Unknown venue scope '{scope}' for {subject}; the row was stored with the Phì Lũ scope.");
        return MenuVenueScopes.PhiLu;
    }

    /// <summary>
    /// Validates a price quote kind and falls back to "unspecified" so a typo
    /// never turns into an invented price.
    /// </summary>
    private static string ResolvePriceType(string? priceType, MenuSeedReport report, string subject)
    {
        if (MenuPriceTypes.IsKnown(priceType))
        {
            return priceType!;
        }

        report.Errors.Add(
            $"Unknown price type '{priceType}' for {subject}; the row was stored as 'unspecified'.");
        return MenuPriceTypes.Unspecified;
    }

    private static void ReportPriceConflict(
        MenuSeedReport report,
        string subject,
        decimal? existingFrom,
        decimal? existingTo,
        decimal? seedFrom,
        decimal? seedTo)
    {
        if (existingFrom != seedFrom || existingTo != seedTo)
        {
            report.Conflicts.Add(
                $"{subject} already exists with price {DescribePrice(existingFrom, existingTo)}; the seed file says {DescribePrice(seedFrom, seedTo)}. The stored price was kept.");
        }
    }

    private static string DescribePrice(decimal? from, decimal? to)
    {
        if (from is null && to is null)
        {
            return "none";
        }

        if (to is null)
        {
            return from!.Value.ToString("0");
        }

        return $"{from!.Value:0}–{to.Value:0}";
    }
}
