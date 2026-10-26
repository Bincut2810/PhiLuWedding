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
        Assert.Equal(4, second.Kept("tiers"));
        Assert.Equal(9, second.Kept("options"));

        // 6 + 6 + 6 + 6 + 6 + 6 + 6 + 7 + 7 dish links.
        Assert.Equal(56, second.Kept("dishLinks"));
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
    public async Task Publication_status_and_notes_are_not_reset()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-04");
        tier.IsPublished = false;
        tier.Description = "Ghi chú do quản trị viên thêm.";

        var note = await db.MenuPriceNotes.FirstAsync(n => n.Key == "price-note-provisional-validity");
        note.IsPublished = false;
        note.Content = "Ghi chú của quản trị viên.";

        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var afterTier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-04");
        Assert.False(afterTier.IsPublished);
        Assert.Equal("Ghi chú do quản trị viên thêm.", afterTier.Description);

        var afterNote = await db.MenuPriceNotes.FirstAsync(n => n.Key == "price-note-provisional-validity");
        Assert.False(afterNote.IsPublished);
        Assert.Equal("Ghi chú của quản trị viên.", afterNote.Content);
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
    public async Task A_dish_that_is_no_longer_used_is_not_removed()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // A dish of a price tier that is no longer part of the public menu.
        // It is not referenced by any option any more, but it must stay in
        // the database.
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

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        Assert.True(await db.Dishes.AnyAsync(d => d.Id == unusedDish.Id));
    }

    [Fact]
    public async Task The_seeder_never_creates_the_removed_supporting_sections()
    {
        var db = TestDbContextFactory.Create();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(0, await db.MenuServices.CountAsync());
        Assert.Equal(0, await db.MenuBeverages.CountAsync());
        Assert.Equal(0, await db.MenuAppetizerNuts.CountAsync());
        Assert.Equal(0, await db.MenuPromotions.CountAsync());
        Assert.Equal(0, await db.MenuPromotionBenefits.CountAsync());
        Assert.Equal(0, await db.MenuPromotionTerms.CountAsync());
        Assert.Equal(0, await db.MenuDepositTerms.CountAsync());

        // Running the seed again changes nothing.
        var second = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        Assert.Equal(0, second.Created("services"));
        Assert.Equal(0, second.Created("beverages"));
        Assert.Equal(0, second.Created("nuts"));
        Assert.Equal(0, second.Created("promotions"));
        Assert.Equal(0, second.Created("promotionTerms"));
        Assert.Equal(0, second.Created("depositTerms"));
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
        Assert.Equal(4, await db.MenuTiers.CountAsync());
        Assert.Equal(9, await db.MenuCombos.CountAsync(c => c.OptionCode != null));
        Assert.Equal(4, report.Created("tiers"));
        Assert.Equal(9, report.Created("options"));
    }

    [Fact]
    public async Task Missing_records_are_added_without_replacing_unrelated_records()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // Remove one tier and its options to simulate a partial seed.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-04");
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
        Assert.Equal(4, await db.MenuTiers.CountAsync());
        Assert.Equal(9, await db.MenuCombos.CountAsync(c => c.OptionCode != null));
        Assert.Equal(1, report.Created("tiers"));
        Assert.Equal(2, report.Created("options"));

        // The dishes were reused, not duplicated.
        Assert.Equal(0, report.Created("dishes"));
    }

    [Fact]
    public async Task A_tierless_option_with_the_catalog_slug_is_linked_to_its_tier()
    {
        var db = TestDbContextFactory.Create();

        var menuSet = new MenuSet
        {
            Name = "Thực đơn tiệc cưới",
            Slug = "menu-tiec-cuoi",
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuSets.Add(menuSet);
        await db.SaveChangesAsync();

        // The option exists, but the link to its price tier is missing.
        db.MenuCombos.Add(new MenuCombo
        {
            MenuSetId = menuSet.Id,
            OptionCode = "A",
            OptionSortOrder = 10,
            Name = "Thực đơn 2.500.000 đồng/bàn - Phương án A",
            Slug = "menu-tiec-cuoi-tier-02-a",
            SortOrder = 1010,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var report = await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var option = await db.MenuCombos
            .Include(c => c.MenuTier!)
            .SingleAsync(c => c.Slug == "menu-tiec-cuoi-tier-02-a");

        Assert.Equal("tier-02", option.MenuTier!.Key);
        Assert.Contains(report.Conflicts, c => c.Contains("tier-02") && c.Contains("link"));
    }

    [Fact]
    public async Task An_ambiguous_tierless_option_is_not_attached_to_a_tier()
    {
        var db = TestDbContextFactory.Create();

        var menuSet = new MenuSet
        {
            Name = "Thực đơn tiệc cưới",
            Slug = "menu-tiec-cuoi",
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuSets.Add(menuSet);
        await db.SaveChangesAsync();

        // A legacy row of the wedding menu set with an option code but
        // without a price tier. Its tier cannot be verified.
        db.MenuCombos.Add(new MenuCombo
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
        });

        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // The legacy row keeps its state; it was not attached to a tier.
        var legacy = await db.MenuCombos.SingleAsync(c => c.Slug == "phuong-an-a-legacy");
        Assert.Null(legacy.MenuTierId);

        // The catalog option was created as a row of its own.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-02");
        var option = await db.MenuCombos.FirstAsync(c => c.MenuTierId == tier.Id && c.OptionCode == "A");
        Assert.Equal("menu-tiec-cuoi-tier-02-a", option.Slug);
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

        // Options.
        var options = await db.MenuCombos.Where(c => c.OptionCode != null).ToListAsync();
        Assert.Equal(catalog.Tiers.Sum(t => t.Options.Count), options.Count);

        // The removed supporting sections stay empty.
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
            .Select(l => $"{l.MenuComboId}:{l.DishId}:{l.SortOrder}:{l.Quantity}")
            .ToListAsync();
        var notes = await db.MenuPriceNotes.OrderBy(n => n.Key).Select(n => $"{n.Key}:{n.ValidUntil}").ToListAsync();

        return string.Join("\n", tiers.Concat(options).Concat(dishes).Concat(links).Concat(notes));
    }
}
