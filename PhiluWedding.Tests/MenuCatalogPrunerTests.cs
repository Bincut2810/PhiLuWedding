using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Tests for the opt-in menu catalog cleanup
/// (<c>MenuCatalogPruner</c> / <c>--prune-menu-catalog</c>).
///
/// <para>
/// The cleanup only clears publication flags and normalizes the sort order
/// of the retained tiers. It never deletes a row, so it is reversible, and
/// it never changes the publication state of a retained option.
/// </para>
/// </summary>
public class MenuCatalogPrunerTests
{
    private static readonly (string Key, string Slug, decimal Price, int SortOrder)[] ExcludedTiers =
    {
        ("tier-01", "menu-tiec-cuoi-tier-01", 2200000m, 5),
        ("tier-03", "menu-tiec-cuoi-tier-03", 2800000m, 15),
        ("tier-05", "menu-tiec-cuoi-tier-05", 3300000m, 25),
        ("tier-06", "menu-tiec-cuoi-tier-06", 3500000m, 35)
    };

    private static async Task<PhiluWeddingDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        return db;
    }

    /// <summary>
    /// Adds the price tiers and options that were removed from the public
    /// menu, the way they still exist in a database that was seeded before
    /// the catalog was trimmed.
    /// </summary>
    private static async Task AddRemovedTiersAsync(PhiluWeddingDbContext db)
    {
        var menuSet = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);

        foreach (var (key, slug, price, sortOrder) in ExcludedTiers)
        {
            var tier = new MenuTier
            {
                Key = key,
                Name = $"Thực đơn {price:0} đồng/bàn",
                Slug = slug,
                Description = "Mức giá đã bị gỡ khỏi thực đơn công khai.",
                Price = price,
                GuestsPerTable = 10,
                SortOrder = sortOrder,
                VenueScope = MenuVenueScopes.BothVenues,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.MenuTiers.Add(tier);
            await db.SaveChangesAsync();

            foreach (var code in new[] { "A", "B" })
            {
                db.MenuCombos.Add(new MenuCombo
                {
                    MenuSetId = menuSet.Id,
                    MenuTierId = tier.Id,
                    OptionCode = code,
                    OptionSortOrder = code == "A" ? 10 : 20,
                    Name = $"Thực đơn {price:0} đồng/bàn - Phương án {code}",
                    Slug = $"{slug}-{code.ToLowerInvariant()}",
                    SortOrder = sortOrder * 100 + (code == "A" ? 10 : 20),
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
    }

    private static async Task AddSupportingSectionsAsync(PhiluWeddingDbContext db)
    {
        db.MenuServices.Add(new MenuService
        {
            Key = "service-mc",
            Name = "MC",
            Slug = "mc",
            PriceType = MenuPriceTypes.Fixed,
            PriceFrom = 1000000m,
            SortOrder = 10,
            VenueScope = MenuVenueScopes.PhiLu,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.MenuBeverages.Add(new MenuBeverage
        {
            Key = "beverage-suoi-dasani",
            Name = "Suối Dasani",
            Slug = "suoi-dasani",
            PriceBelowThreshold = 10000m,
            PriceAboveThreshold = 9000m,
            SortOrder = 10,
            VenueScope = MenuVenueScopes.PhiLu,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.MenuAppetizerNuts.Add(new MenuAppetizerNut
        {
            Key = "nut-hat-dua",
            Name = "Hạt dưa",
            Slug = "hat-dua",
            PriceType = MenuPriceTypes.TimeDependent,
            SortOrder = 10,
            VenueScope = MenuVenueScopes.PhiLu,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.MenuPromotions.Add(new MenuPromotion
        {
            Key = "promo-14-24",
            Name = "Ưu đãi 14–24 bàn",
            Slug = "uu-dai-14-24",
            RangeLabel = "14–24 bàn",
            MinTableCount = 14,
            MaxTableCount = 24,
            SortOrder = 10,
            VenueScope = MenuVenueScopes.PhiLu,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.MenuPromotionTerms.Add(new MenuPromotionTerm
        {
            Key = "promotion-term-booked-tables",
            Slug = "promotion-term-booked-tables",
            Content = "Khuyến mãi chỉ áp dụng cho số bàn đã đặt chính thức.",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.MenuDepositTerms.Add(new MenuDepositTerm
        {
            Key = "deposit-first-amber",
            VenueScope = MenuVenueScopes.PhiLu,
            HallSlug = "philu-sanh-amber",
            HallName = "Sảnh Amber",
            Stage = DepositStage.First,
            Amount = 12000000m,
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task The_cleanup_unpublishes_the_four_excluded_tiers()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        var report = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(4, report.Updated("excludedTiers"));

        foreach (var (key, _, _, _) in ExcludedTiers)
        {
            var tier = await db.MenuTiers.FirstAsync(t => t.Key == key);
            Assert.False(tier.IsPublished);
        }
    }

    [Fact]
    public async Task The_cleanup_unpublishes_the_eight_excluded_options()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var excludedOptions = await db.MenuCombos
            .Include(c => c.MenuTier!)
            .Where(c => c.MenuTier!.Key != null
                && (c.MenuTier.Key == "tier-01"
                    || c.MenuTier.Key == "tier-03"
                    || c.MenuTier.Key == "tier-05"
                    || c.MenuTier.Key == "tier-06"))
            .ToListAsync();

        Assert.Equal(8, excludedOptions.Count);
        Assert.All(excludedOptions, o => Assert.False(o.IsPublished));
    }

    [Fact]
    public async Task The_cleanup_keeps_the_three_options_of_the_five_million_tier_published()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-08");
        var options = await db.MenuCombos
            .Where(c => c.MenuTierId == tier.Id)
            .OrderBy(c => c.OptionCode)
            .ToListAsync();

        Assert.Equal(3, options.Count);
        Assert.Equal(new[] { "A", "B", "C" }, options.Select(o => o.OptionCode).ToArray());
        Assert.All(options, o => Assert.True(o.IsPublished));
    }

    [Fact]
    public async Task The_cleanup_never_resets_the_publication_state_of_a_retained_option()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        // An administrator unpublished a retained option on purpose.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-02");
        var option = await db.MenuCombos.FirstAsync(c => c.MenuTierId == tier.Id && c.OptionCode == "A");
        option.IsPublished = false;
        await db.SaveChangesAsync();

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuCombos.FirstAsync(c => c.Id == option.Id);
        Assert.False(after.IsPublished);
    }

    [Fact]
    public async Task Running_the_cleanup_twice_produces_the_same_state()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);
        await AddSupportingSectionsAsync(db);

        var first = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);
        Assert.True(first.TotalChanges > 0);

        var stateAfterFirst = await SnapshotAsync(db);

        var second = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(0, second.TotalChanges);
        Assert.Equal(stateAfterFirst, await SnapshotAsync(db));
    }

    [Fact]
    public async Task The_cleanup_never_deletes_dishes_images_or_dish_links()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        var dishCount = await db.Dishes.CountAsync();
        var linkCount = await db.MenuComboDishes.CountAsync();
        var comboCount = await db.MenuCombos.CountAsync();
        var tierCount = await db.MenuTiers.CountAsync();

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(dishCount, await db.Dishes.CountAsync());
        Assert.Equal(linkCount, await db.MenuComboDishes.CountAsync());
        Assert.Equal(comboCount, await db.MenuCombos.CountAsync());
        Assert.Equal(tierCount, await db.MenuTiers.CountAsync());
        Assert.Equal(0, await db.DishImages.CountAsync());
    }

    [Fact]
    public async Task The_cleanup_unpublishes_the_supporting_sections_without_deleting_them()
    {
        var db = await SeedAsync();
        await AddSupportingSectionsAsync(db);

        var report = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(1, report.Updated("services"));
        Assert.Equal(1, report.Updated("beverages"));
        Assert.Equal(1, report.Updated("nuts"));
        Assert.Equal(1, report.Updated("promotions"));
        Assert.Equal(1, report.Updated("promotionTerms"));
        Assert.Equal(1, report.Updated("depositTerms"));

        // The rows are still there, they are only unpublished.
        Assert.Equal(1, await db.MenuServices.CountAsync());
        Assert.Equal(1, await db.MenuBeverages.CountAsync());
        Assert.Equal(1, await db.MenuAppetizerNuts.CountAsync());
        Assert.Equal(1, await db.MenuPromotions.CountAsync());
        Assert.Equal(1, await db.MenuPromotionTerms.CountAsync());
        Assert.Equal(1, await db.MenuDepositTerms.CountAsync());

        Assert.False((await db.MenuServices.SingleAsync()).IsPublished);
        Assert.False((await db.MenuBeverages.SingleAsync()).IsPublished);
        Assert.False((await db.MenuAppetizerNuts.SingleAsync()).IsPublished);
        Assert.False((await db.MenuPromotions.SingleAsync()).IsPublished);
        Assert.False((await db.MenuPromotionTerms.SingleAsync()).IsPublished);
        Assert.False((await db.MenuDepositTerms.SingleAsync()).IsPublished);
    }

    [Fact]
    public async Task The_cleanup_reports_missing_rows_instead_of_creating_them()
    {
        var db = await SeedAsync();

        // The excluded tiers do not exist in this database.
        var report = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.True(report.Missing("excludedTiers") > 0);
        Assert.Equal(4, await db.MenuTiers.CountAsync());
        Assert.Equal(9, await db.MenuCombos.CountAsync(c => c.OptionCode != null));
    }

    [Fact]
    public async Task The_cleanup_does_not_attach_a_tierless_option_to_a_tier()
    {
        var db = await SeedAsync();

        var menuSet = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);

        var legacy = new MenuCombo
        {
            MenuSetId = menuSet.Id,
            OptionCode = "A",
            OptionSortOrder = 10,
            Name = "Phương án A (legacy)",
            Slug = "phuong-an-a-legacy",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuCombos.Add(legacy);
        await db.SaveChangesAsync();

        var report = await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuCombos.SingleAsync(c => c.Id == legacy.Id);
        Assert.Null(after.MenuTierId);
        Assert.Contains(report.Conflicts, c => c.Contains("belongs to no price tier"));
    }

    [Fact]
    public async Task The_cleanup_normalizes_the_sort_order_of_the_retained_tiers()
    {
        var db = await SeedAsync();

        foreach (var tier in await db.MenuTiers.ToListAsync())
        {
            tier.SortOrder = 70;
        }

        await db.SaveChangesAsync();

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var tiers = await db.MenuTiers.OrderBy(t => t.SortOrder).ToListAsync();

        Assert.Equal(
            new[] { "tier-02", "tier-04", "tier-07", "tier-08" },
            tiers.Select(t => t.Key).ToArray());
        Assert.Equal(new[] { 10, 20, 30, 40 }, tiers.Select(t => t.SortOrder).ToArray());
    }

    [Fact]
    public async Task The_cleanup_updates_the_wedding_menu_set_description()
    {
        var db = await SeedAsync();

        var menuSet = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);
        menuSet.Description = "Giá theo bàn 10 khách. Tám mức giá, mỗi mức có phương án A và B.";
        await db.SaveChangesAsync();

        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);

        Assert.NotNull(after.Description);
        Assert.Contains("Bốn mức giá", after.Description!);
        Assert.DoesNotContain("Tám mức giá", after.Description!);
    }

    [Fact]
    public async Task The_cleanup_does_not_delete_the_dishes_of_the_removed_tiers()
    {
        var db = await SeedAsync();

        // A dish that only the removed price tiers used.
        var unusedDish = new Dish
        {
            Name = "Món của mức giá đã bị gỡ",
            Slug = "mon-cua-muc-gia-da-bi-go",
            SortOrder = 9999,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Dishes.Add(unusedDish);
        await db.SaveChangesAsync();

        await AddRemovedTiersAsync(db);
        await MenuCatalogPruner.PruneAsync(db, TestDbContextFactory.Logger);

        Assert.True(await db.Dishes.AnyAsync(d => d.Id == unusedDish.Id));
    }

    [Fact]
    public async Task The_seeder_does_not_unpublish_the_excluded_tiers()
    {
        var db = await SeedAsync();
        await AddRemovedTiersAsync(db);

        // The cleanup is not coupled to the seed: seeding leaves the
        // publication state of every row alone.
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-01");
        Assert.True(tier.IsPublished);
    }

    private static async Task<string> SnapshotAsync(PhiluWeddingDbContext db)
    {
        var tiers = await db.MenuTiers
            .OrderBy(t => t.Key)
            .Select(t => $"{t.Key}:{t.IsPublished}:{t.SortOrder}")
            .ToListAsync();

        var options = await db.MenuCombos
            .OrderBy(c => c.Id)
            .Select(c => $"{c.Id}:{c.OptionCode}:{c.IsPublished}:{c.MenuTierId}")
            .ToListAsync();

        var dishes = await db.Dishes.OrderBy(d => d.Id).Select(d => $"{d.Id}:{d.Name}").ToListAsync();

        var links = await db.MenuComboDishes
            .OrderBy(l => l.MenuComboId).ThenBy(l => l.SortOrder)
            .Select(l => $"{l.MenuComboId}:{l.DishId}:{l.SortOrder}:{l.Quantity}")
            .ToListAsync();

        var services = await db.MenuServices.OrderBy(s => s.Key).Select(s => $"{s.Key}:{s.IsPublished}").ToListAsync();
        var beverages = await db.MenuBeverages.OrderBy(b => b.Key).Select(b => $"{b.Key}:{b.IsPublished}").ToListAsync();
        var nuts = await db.MenuAppetizerNuts.OrderBy(n => n.Key).Select(n => $"{n.Key}:{n.IsPublished}").ToListAsync();
        var promotions = await db.MenuPromotions.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.IsPublished}").ToListAsync();
        var terms = await db.MenuPromotionTerms.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.IsPublished}").ToListAsync();
        var deposits = await db.MenuDepositTerms.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.IsPublished}").ToListAsync();

        return string.Join("\n", tiers.Concat(options).Concat(dishes).Concat(links)
            .Concat(services).Concat(beverages).Concat(nuts).Concat(promotions)
            .Concat(terms).Concat(deposits));
    }
}
