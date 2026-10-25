using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Tests that the menu catalog seed is idempotent: running it twice creates
/// no duplicates, never resets an edited price and never deletes existing
/// content.
/// </summary>
public class MenuCatalogIdempotencyTests
{
    [Fact]
    public async Task Seeding_twice_creates_no_duplicates()
    {
        var db = TestDbContextFactory.Create();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        var afterFirst = await SnapshotAsync(db);

        var second = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        var afterSecond = await SnapshotAsync(db);

        Assert.Equal(afterFirst, afterSecond);
        Assert.Empty(second.Conflicts);
        Assert.Empty(second.Errors);
        Assert.Equal(0, second.Created("tiers"));
        Assert.Equal(0, second.Created("options"));
        Assert.Equal(0, second.Created("dishes"));
        Assert.Equal(0, second.Created("dishLinks"));
        Assert.Equal(8, second.Kept("tiers"));
        Assert.Equal(17, second.Kept("options"));
        Assert.Equal(104, second.Kept("dishLinks"));
    }

    [Fact]
    public async Task An_edited_price_is_not_reset_by_reseeding()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // Simulate an administrator editing the price of a tier.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-08");
        tier.Price = 5555555m;
        await db.SaveChangesAsync();

        var report = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuTiers.FirstAsync(t => t.Key == "tier-08");
        Assert.Equal(5555555m, after.Price);

        // The difference is reported rather than silently applied.
        Assert.Contains(report.Conflicts, c => c.Contains("tier-08") && c.Contains("5555555"));
    }

    [Fact]
    public async Task An_edited_service_price_is_not_reset_by_reseeding()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var service = await db.MenuServices.FirstAsync(s => s.Key == "service-mc");
        service.PriceFrom = 1234000m;
        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuServices.FirstAsync(s => s.Key == "service-mc");
        Assert.Equal(1234000m, after.PriceFrom);
    }

    [Fact]
    public async Task An_edited_beverage_price_is_not_reset_by_reseeding()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var beverage = await db.MenuBeverages.FirstAsync(b => b.Key == "beverage-suoi-dasani");
        beverage.PriceBelowThreshold = 11000m;
        beverage.PriceAboveThreshold = 10000m;
        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var after = await db.MenuBeverages.FirstAsync(b => b.Key == "beverage-suoi-dasani");
        Assert.Equal(11000m, after.PriceBelowThreshold);
        Assert.Equal(10000m, after.PriceAboveThreshold);
    }

    [Fact]
    public async Task Publication_status_and_notes_are_not_reset()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-05");
        tier.IsPublished = false;
        tier.Description = "Ghi chú do quản trị viên thêm.";

        var service = await db.MenuServices.FirstAsync(s => s.Key == "service-mc");
        service.IsPublished = false;
        service.Note = "Ghi chú của quản trị viên.";

        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var afterTier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-05");
        Assert.False(afterTier.IsPublished);
        Assert.Equal("Ghi chú do quản trị viên thêm.", afterTier.Description);

        var afterService = await db.MenuServices.FirstAsync(s => s.Key == "service-mc");
        Assert.False(afterService.IsPublished);
        Assert.Equal("Ghi chú của quản trị viên.", afterService.Note);
    }

    [Fact]
    public async Task Existing_menu_content_is_not_deleted()
    {
        var db = TestDbContextFactory.Create();

        // Content that already exists before the catalog seed runs.
        var menuSet = new MenuSet
        {
            Name = "Bộ thực đơn riêng của nhà hàng",
            Slug = "menu-rieng",
            Description = "Nội dung có sẵn, không được xóa.",
            SortOrder = 99,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.MenuSets.Add(menuSet);
        await db.SaveChangesAsync();

        var dish = new Dish
        {
            Name = "Món riêng của nhà hàng",
            Slug = "mon-rieng",
            SortOrder = 999,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Dishes.Add(dish);
        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        Assert.True(await db.MenuSets.AnyAsync(s => s.Slug == "menu-rieng"));
        Assert.True(await db.Dishes.AnyAsync(d => d.Slug == "mon-rieng"));
        Assert.Equal(2, await db.MenuSets.CountAsync());
    }

    [Fact]
    public async Task One_existing_menu_record_does_not_block_the_rest_of_the_catalog()
    {
        var db = TestDbContextFactory.Create();

        // A single unrelated menu row exists before the seed runs.
        db.MenuSets.Add(new MenuSet
        {
            Name = "MENU DEMO",
            Slug = "menu-demo",
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var report = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // The whole catalog was still seeded.
        Assert.Equal(8, await db.MenuTiers.CountAsync());
        Assert.Equal(17, await db.MenuCombos.CountAsync(c => c.OptionCode != null));
        Assert.Equal(8, report.Created("tiers"));
        Assert.Equal(17, report.Created("options"));
    }

    [Fact]
    public async Task Missing_records_are_added_without_replacing_unrelated_records()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // Remove one tier and its options to simulate a partial seed.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-06");
        var optionIds = await db.MenuCombos
            .Where(c => c.MenuTierId == tier.Id)
            .Select(c => c.Id)
            .ToListAsync();

        db.MenuComboDishes.RemoveRange(db.MenuComboDishes.Where(l => optionIds.Contains(l.MenuComboId)));
        db.MenuCombos.RemoveRange(db.MenuCombos.Where(c => optionIds.Contains(c.Id)));
        db.MenuTiers.Remove(tier);
        await db.SaveChangesAsync();

        var report = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // The missing tier is back, and everything else is untouched.
        Assert.Equal(8, await db.MenuTiers.CountAsync());
        Assert.Equal(17, await db.MenuCombos.CountAsync(c => c.OptionCode != null));
        Assert.Equal(1, report.Created("tiers"));
        Assert.Equal(2, report.Created("options"));

        // The dishes were reused, not duplicated.
        Assert.Equal(0, report.Created("dishes"));
    }

    [Fact]
    public async Task The_cumulative_promotion_benefit_list_stays_deterministic_after_reseeding()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var before = await BuildCumulativeAsync(db);
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        var after = await BuildCumulativeAsync(db);

        Assert.Equal(before, after);
        Assert.Equal(28, after.Count);
    }

    [Fact]
    public async Task The_seed_file_matches_what_the_database_contains()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var catalog = MenuCatalogSeeder.LoadCatalog();

        // Tiers, prices and option codes.
        var tiers = await db.MenuTiers.OrderBy(t => t.SortOrder).ToListAsync();
        Assert.Equal(catalog.Tiers.Count, tiers.Count);

        for (var i = 0; i < catalog.Tiers.Count; i++)
        {
            Assert.Equal(catalog.Tiers[i].Key, tiers[i].Key);
            Assert.Equal(catalog.Tiers[i].Price, tiers[i].Price);
            Assert.Equal(catalog.Tiers[i].Name, tiers[i].Name);
        }

        // Sections.
        Assert.Equal(catalog.Services.Count, await db.MenuServices.CountAsync());
        Assert.Equal(catalog.Beverages.Count, await db.MenuBeverages.CountAsync());
        Assert.Equal(catalog.Nuts.Count, await db.MenuAppetizerNuts.CountAsync());
        Assert.Equal(catalog.Promotions.Count, await db.MenuPromotions.CountAsync());
        Assert.Equal(catalog.PromotionTerms.Count, await db.MenuPromotionTerms.CountAsync());
        Assert.Equal(catalog.DepositTerms.Count, await db.MenuDepositTerms.CountAsync());
        Assert.Equal(catalog.PriceNotes.Count, await db.MenuPriceNotes.CountAsync());
    }

    private static async Task<string> SnapshotAsync(PhiluWeddingDbContext db)
    {
        var tiers = await db.MenuTiers.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.Price}").ToListAsync();
        var options = await db.MenuCombos
            .Where(c => c.OptionCode != null)
            .OrderBy(c => c.MenuTierId).ThenBy(c => c.OptionCode)
            .Select(c => $"{c.MenuTierId}:{c.OptionCode}:{c.Name}")
            .ToListAsync();
        var dishes = await db.Dishes.OrderBy(d => d.Id).Select(d => $"{d.Name}|{d.Slug}").ToListAsync();
        var links = await db.MenuComboDishes
            .OrderBy(l => l.MenuComboId).ThenBy(l => l.SortOrder)
            .Select(l => $"{l.MenuComboId}:{l.DishId}:{l.SortOrder}")
            .ToListAsync();
        var services = await db.MenuServices.OrderBy(s => s.Key).Select(s => $"{s.Key}:{s.PriceFrom}:{s.PriceTo}").ToListAsync();
        var beverages = await db.MenuBeverages.OrderBy(b => b.Key).Select(b => $"{b.Key}:{b.PriceBelowThreshold}:{b.PriceAboveThreshold}").ToListAsync();
        var nuts = await db.MenuAppetizerNuts.OrderBy(n => n.Key).Select(n => $"{n.Key}:{n.PriceType}:{n.Price}").ToListAsync();
        var promotions = await db.MenuPromotions.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.MinTableCount}:{p.MaxTableCount}").ToListAsync();
        var deposits = await db.MenuDepositTerms.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.Amount}:{t.Stage}").ToListAsync();
        var notes = await db.MenuPriceNotes.OrderBy(n => n.Key).Select(n => $"{n.Key}:{n.ValidUntil}").ToListAsync();

        return string.Join("\n", tiers.Concat(options).Concat(dishes).Concat(links)
            .Concat(services).Concat(beverages).Concat(nuts).Concat(promotions)
            .Concat(deposits).Concat(notes));
    }

    private static async Task<List<string>> BuildCumulativeAsync(PhiluWeddingDbContext db)
    {
        var promotions = await db.MenuPromotions
            .Include(p => p.Benefits)
            .Include(p => p.BasePromotion!)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var top = promotions.Last();
        var chain = new List<MenuPromotion>();
        var current = top;

        while (current is not null)
        {
            chain.Add(current);
            current = current.BasePromotion;
        }

        chain.Reverse();

        return chain
            .SelectMany(p => p.Benefits.OrderBy(b => b.SortOrder).Select(b => b.Text))
            .ToList();
    }
}
