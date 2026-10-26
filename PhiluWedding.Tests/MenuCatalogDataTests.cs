using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Data-integrity tests for the fixed wedding-menu catalog: the four public
/// price tiers, the nine public options, their ordered dish lists and the
/// supporting sections that are no longer part of the public menu.
/// </summary>
public class MenuCatalogDataTests
{
    private static readonly MenuCatalogDocument Catalog = MenuCatalogSeeder.LoadCatalog();

    private static async Task<PhiluWeddingDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        return db;
    }

    [Fact]
    public async Task Exactly_four_price_tiers_exist()
    {
        var db = await SeedAsync();
        var tiers = await db.MenuTiers.OrderBy(t => t.SortOrder).ToListAsync();

        Assert.Equal(4, tiers.Count);
        Assert.Equal(
            new[] { "tier-02", "tier-04", "tier-07", "tier-08" },
            tiers.Select(t => t.Key).ToArray());
        Assert.Equal(
            new[] { 2500000m, 3000000m, 4000000m, 5000000m },
            tiers.Select(t => t.Price).ToArray());
        Assert.All(tiers, t => Assert.Equal(10, t.GuestsPerTable));
    }

    [Fact]
    public async Task The_four_price_tiers_are_ordered_by_price_without_gaps()
    {
        var db = await SeedAsync();
        var tiers = await db.MenuTiers.OrderBy(t => t.SortOrder).ToListAsync();

        // The sort orders follow the catalog, so the prices are shown in
        // ascending order and the removed tiers leave no gaps behind.
        Assert.Equal(new[] { 10, 20, 30, 40 }, tiers.Select(t => t.SortOrder).ToArray());
        Assert.Equal(tiers.Select(t => t.Price).OrderBy(p => p), tiers.Select(t => t.Price));
    }

    [Fact]
    public async Task Every_price_tier_applies_to_both_venues()
    {
        var db = await SeedAsync();
        var tiers = await db.MenuTiers.ToListAsync();

        Assert.All(tiers, t => Assert.Equal(MenuVenueScopes.BothVenues, t.VenueScope));
        Assert.All(tiers, t => Assert.True(t.IsPublished));
    }

    [Fact]
    public async Task There_are_nine_options_with_the_expected_codes()
    {
        var db = await SeedAsync();
        var options = await db.MenuCombos
            .Where(c => c.OptionCode != null)
            .Include(c => c.MenuTier!)
            .ToListAsync();

        Assert.Equal(9, options.Count);
        Assert.Equal(4, options.Count(o => o.OptionCode == "A"));
        Assert.Equal(4, options.Count(o => o.OptionCode == "B"));
        Assert.Equal(1, options.Count(o => o.OptionCode == "C"));

        // Every option belongs to exactly one tier.
        Assert.All(options, o => Assert.NotNull(o.MenuTier));
        Assert.Equal(4, options.Select(o => o.MenuTierId).Distinct().Count());
    }

    [Fact]
    public async Task The_first_three_price_tiers_sell_the_options_a_and_b()
    {
        var db = await SeedAsync();

        foreach (var key in new[] { "tier-02", "tier-04", "tier-07" })
        {
            var tier = await db.MenuTiers.FirstAsync(t => t.Key == key);
            var codes = await db.MenuCombos
                .Where(c => c.MenuTierId == tier.Id)
                .OrderBy(c => c.OptionCode)
                .Select(c => c.OptionCode)
                .ToListAsync();

            Assert.Equal(new[] { "A", "B" }, codes);
        }
    }

    [Fact]
    public async Task Option_c_belongs_to_the_five_million_tier_and_shares_its_price()
    {
        var db = await SeedAsync();
        var optionC = await db.MenuCombos
            .Include(c => c.MenuTier!)
            .FirstAsync(c => c.OptionCode == "C");

        Assert.Equal("tier-08", optionC.MenuTier!.Key);
        Assert.Equal(5000000m, optionC.MenuTier.Price);

        // The option itself carries no separate price of its own.
        Assert.Null(optionC.Price);

        // All three options of the tier are published and share the price.
        var tierEightOptions = await db.MenuCombos
            .Where(c => c.MenuTierId == optionC.MenuTierId)
            .Include(c => c.MenuTier!)
            .ToListAsync();

        Assert.Equal(3, tierEightOptions.Count);
        Assert.Equal(
            new[] { "A", "B", "C" },
            tierEightOptions.OrderBy(o => o.OptionCode).Select(o => o.OptionCode).ToArray());
        Assert.All(tierEightOptions, o => Assert.True(o.IsPublished));
        Assert.All(tierEightOptions, o => Assert.Equal(5000000m, o.MenuTier!.Price));
        Assert.All(tierEightOptions, o => Assert.Null(o.Price));
    }

    [Fact]
    public async Task The_catalog_holds_no_entries_for_the_excluded_price_tiers()
    {
        // The seed file itself must not mention the removed tiers.
        Assert.DoesNotContain(
            Catalog.Tiers,
            t => t.Key is "tier-01" or "tier-03" or "tier-05" or "tier-06");

        Assert.Equal(4, Catalog.Tiers.Count);
        Assert.Equal(9, Catalog.Tiers.Sum(t => t.Options.Count));

        // And the database holds exactly the four public tiers.
        var db = await SeedAsync();
        var keys = await db.MenuTiers.Select(t => t.Key).ToListAsync();

        Assert.Equal(4, keys.Count);
        Assert.DoesNotContain(keys, k => k is "tier-01" or "tier-03" or "tier-05" or "tier-06");
    }

    [Fact]
    public async Task Every_option_has_its_correct_ordered_dish_list()
    {
        var db = await SeedAsync();

        var options = await db.MenuCombos
            .Where(c => c.OptionCode != null)
            .Include(c => c.MenuTier!)
            .Include(c => c.ComboDishes)
            .ToListAsync();

        foreach (var tierDefinition in Catalog.Tiers)
        {
            foreach (var optionDefinition in tierDefinition.Options)
            {
                var option = options.Single(o =>
                    o.MenuTier!.Key == tierDefinition.Key &&
                    o.OptionCode == optionDefinition.Code);

                var dishNames = option.ComboDishes
                    .OrderBy(l => l.SortOrder)
                    .Select(l => l.Dish!.Name)
                    .ToList();

                // The catalog lists the same dish under two capitalisations
                // ("Khai vị Tứ quý" and "Khai vị Tứ Quý"). They are one dish
                // in the global catalog, so the comparison ignores case.
                Assert.Equal(
                    optionDefinition.Dishes.Select(d => d.ToLowerInvariant()).ToList(),
                    dishNames.Select(d => d.ToLowerInvariant()).ToList());

                // The order values are a gapless 1..n sequence.
                var orders = option.ComboDishes
                    .OrderBy(l => l.SortOrder)
                    .Select(l => l.SortOrder)
                    .ToList();
                Assert.Equal(Enumerable.Range(1, optionDefinition.Dishes.Count).ToList(), orders);

                // The quantity of every dish is the source value: one.
                Assert.All(option.ComboDishes, l => Assert.Equal(1, l.Quantity));
            }
        }
    }

    [Fact]
    public async Task The_same_dish_may_appear_in_several_options_without_duplicate_rows()
    {
        var db = await SeedAsync();

        // "Trái cây" appears in the options A and B of tier 2, in the
        // options A and B of tier 4 and in the option B of tier 7.
        var traiCay = await db.Dishes
            .Include(d => d.ComboDishes)
            .FirstAsync(d => d.Name == "Trái cây");

        var comboIds = traiCay.ComboDishes.Select(l => l.MenuComboId).ToList();

        var options = await db.MenuCombos
            .Where(c => comboIds.Contains(c.Id))
            .Include(c => c.MenuTier!)
            .ToListAsync();

        // Five different options, spread over three price tiers, all point at
        // the very same dish row.
        Assert.Equal(5, options.Count);
        Assert.Equal(3, options.Select(o => o.MenuTier!.Key).Distinct().Count());
        Assert.Equal(5, comboIds.Distinct().Count());

        // The dish exists exactly once in the global catalog.
        Assert.Equal(1, await db.Dishes.CountAsync(d => d.Name == "Trái cây"));
    }

    [Fact]
    public async Task A_dish_written_with_two_capitalisations_is_stored_once()
    {
        var db = await SeedAsync();

        // The catalog writes "Khai vị Tứ quý" and "Khai vị Tứ Quý" in
        // different options. It is one dish, so the catalog keeps a single
        // row and both options reference it.
        var matches = await db.Dishes
            .Where(d => d.Name.StartsWith("Khai vị Tứ"))
            .ToListAsync();

        var dish = Assert.Single(matches);

        var comboIds = await db.MenuComboDishes
            .Where(l => l.DishId == dish.Id)
            .Select(l => l.MenuComboId)
            .ToListAsync();

        // The dish is shared by more than one option, but it exists once.
        Assert.NotEmpty(comboIds);
        Assert.Equal(comboIds.Count, comboIds.Distinct().Count());
    }

    [Fact]
    public async Task An_existing_dish_with_a_different_capitalisation_is_reused()
    {
        var db = TestDbContextFactory.Create();

        // The dish already exists in the database, written with a different
        // capitalisation than the catalog uses.
        db.Dishes.Add(new Dish
        {
            Name = "Khai vị Tứ quý",
            Slug = "khai-vi-tu-quy",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        // The seeder reuses the existing row instead of creating a second one.
        var matches = await db.Dishes
            .Where(d => d.Name.StartsWith("Khai vị Tứ"))
            .ToListAsync();

        var dish = Assert.Single(matches);
        Assert.Equal("Khai vị Tứ quý", dish.Name);
    }

    [Fact]
    public async Task Vietnamese_text_survives_seeding_and_retrieval()
    {
        var db = await SeedAsync();

        var names = await db.Dishes.Select(d => d.Name).ToListAsync();

        Assert.Contains("Gỏi hoa chuối + bpt", names);
        Assert.Contains("Súp bí đao thập cẩm", names);
        Assert.Contains("Cá hồi nướng phomai kèm rau củ", names);
        Assert.Contains("Tôm sú nướng sốt Wasabi", names);
        Assert.Contains("Bò nấu kiểu Ý + bánh mỳ", names);
        Assert.Contains("Khai vị Tứ Quý", names);
        Assert.Contains("Trái cây (02 loại)", names);

        // Slugs stay ASCII, so URLs do not carry raw diacritics.
        var slug = await db.Dishes.Where(d => d.Name == "Gỏi hoa chuối + bpt")
            .Select(d => d.Slug)
            .SingleAsync();
        Assert.Equal("goi-hoa-chuoi-bpt", slug);
    }

    [Fact]
    public async Task Dish_catalog_does_not_contain_unnecessary_duplicates()
    {
        var db = await SeedAsync();

        var expectedDistinct = Catalog.Tiers
            .SelectMany(t => t.Options)
            .SelectMany(o => o.Dishes)
            .Select(d => d.Trim().ToLowerInvariant())
            .Distinct()
            .Count();

        var catalogDishes = await db.Dishes.CountAsync();
        Assert.Equal(expectedDistinct, catalogDishes);
    }

    [Fact]
    public async Task The_catalog_has_no_supporting_sections()
    {
        // The five sections that were removed from the public menu are empty
        // in the seed file ...
        Assert.Empty(Catalog.Services);
        Assert.Empty(Catalog.Beverages);
        Assert.Empty(Catalog.Nuts);
        Assert.Empty(Catalog.Promotions);
        Assert.Empty(Catalog.PromotionTerms);
        Assert.Empty(Catalog.DepositTerms);

        // ... and the seeder therefore creates no such rows.
        var db = await SeedAsync();

        Assert.Equal(0, await db.MenuServices.CountAsync());
        Assert.Equal(0, await db.MenuBeverages.CountAsync());
        Assert.Equal(0, await db.MenuAppetizerNuts.CountAsync());
        Assert.Equal(0, await db.MenuPromotions.CountAsync());
        Assert.Equal(0, await db.MenuPromotionBenefits.CountAsync());
        Assert.Equal(0, await db.MenuPromotionTerms.CountAsync());
        Assert.Equal(0, await db.MenuDepositTerms.CountAsync());
    }

    [Fact]
    public async Task The_price_notes_are_kept()
    {
        var db = await SeedAsync();
        var notes = await db.MenuPriceNotes.OrderBy(n => n.SortOrder).ToListAsync();

        Assert.Equal(2, notes.Count);

        var main = notes[0];
        Assert.Equal(new DateOnly(2026, 8, 31), main.ValidUntil);
        Assert.Contains("tham khảo", main.Content);
        Assert.Contains("8/2026", main.Content);
        Assert.Contains("liên hệ", main.Content);
        Assert.Contains("VAT", main.VatNote);

        // The validity period is never extended to a future date.
        Assert.All(notes, n => Assert.True(n.ValidUntil <= new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public async Task The_wedding_menu_set_describes_four_price_levels()
    {
        var db = await SeedAsync();
        var menuSet = await db.MenuSets.SingleAsync(s => s.Slug == "menu-tiec-cuoi");

        Assert.NotNull(menuSet.Description);
        Assert.Contains("Bốn mức giá", menuSet.Description!);
        Assert.DoesNotContain("Tám mức giá", menuSet.Description!);
    }

    [Fact]
    public async Task The_price_tiers_and_the_menu_set_are_shared_by_both_venues()
    {
        var db = await SeedAsync();

        Assert.All(await db.MenuTiers.ToListAsync(), t => Assert.Equal(MenuVenueScopes.BothVenues, t.VenueScope));

        var menuSet = await db.MenuSets.SingleAsync(s => s.Slug == "menu-tiec-cuoi");
        Assert.Equal(MenuVenueScopes.BothVenues, menuSet.VenueScope);
    }

    [Fact]
    public async Task Prices_are_stored_in_the_database_and_not_in_the_dish_composition()
    {
        var db = await SeedAsync();

        // The price lives on the tier, independently of the dishes.
        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-08");
        Assert.Equal(5000000m, tier.Price);

        // No dish carries a price of its own.
        Assert.All(await db.Dishes.ToListAsync(), d => Assert.Null(d.Price));

        // No option carries a separate price either.
        Assert.All(await db.MenuCombos.ToListAsync(), c => Assert.Null(c.Price));
    }
}
