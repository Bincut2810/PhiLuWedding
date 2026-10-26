using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Pages.ThucDon;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Tests that every public menu route applies the same public scope: the
/// four public price tiers and the nine public options of the wedding menu.
/// The rules live in <see cref="PublicMenuScope"/>; these tests make sure
/// the routes really use them.
/// </summary>
public class MenuPublicRouteTests
{
    private static async Task<PhiluWeddingDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        return db;
    }

    /// <summary>
    /// Adds a price tier and one option that are no longer part of the
    /// public menu, the way they still exist in a database that was seeded
    /// before the catalog was trimmed.
    /// </summary>
    private static async Task<MenuCombo> AddExcludedOptionAsync(PhiluWeddingDbContext db)
    {
        var menuSet = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);

        var tier = new MenuTier
        {
            Key = "tier-01",
            Name = "Thực đơn 2.200.000 đồng/bàn",
            Slug = "menu-tiec-cuoi-tier-01",
            Price = 2200000m,
            GuestsPerTable = 10,
            SortOrder = 5,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuTiers.Add(tier);
        await db.SaveChangesAsync();

        var option = new MenuCombo
        {
            MenuSetId = menuSet.Id,
            MenuTierId = tier.Id,
            OptionCode = "A",
            OptionSortOrder = 10,
            Name = "Thực đơn 2.200.000 đồng/bàn - Phương án A",
            Slug = "menu-tiec-cuoi-tier-01-a",
            SortOrder = 510,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuCombos.Add(option);
        await db.SaveChangesAsync();

        return option;
    }

    [Fact]
    public async Task The_menu_listing_counts_only_the_public_options()
    {
        var db = await SeedAsync();
        await AddExcludedOptionAsync(db);

        var model = new IndexModel(NullLogger<IndexModel>.Instance, db);
        await model.OnGetAsync();

        var weddingMenu = Assert.Single(model.MenuSets, m => m.Slug == PublicMenuScope.WeddingMenuSetSlug);

        // The excluded option does not inflate the count.
        Assert.Equal(9, weddingMenu.ComboCount);
    }

    [Fact]
    public async Task The_menu_set_detail_lists_only_the_public_options()
    {
        var db = await SeedAsync();
        var excluded = await AddExcludedOptionAsync(db);

        var menuSetId = await db.MenuSets
            .Where(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug)
            .Select(s => s.Id)
            .SingleAsync();

        var model = new DetailsModel(new StubImageResolver(), NullLogger<DetailsModel>.Instance, db);
        var result = await model.OnGetAsync(menuSetId);

        Assert.IsType<PageResult>(result);

        var menuSet = model.MenuSet;
        Assert.NotNull(menuSet);
        Assert.Equal(9, menuSet!.Combos.Count);
        Assert.DoesNotContain(menuSet.Combos, c => c.Id == excluded.Id);
    }

    [Fact]
    public async Task A_request_for_an_excluded_option_is_not_found()
    {
        var db = await SeedAsync();
        var excluded = await AddExcludedOptionAsync(db);

        var model = new ComboDetailsModel(new StubImageResolver(), NullLogger<ComboDetailsModel>.Instance, db);
        var result = await model.OnGetAsync(excluded.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task A_request_for_a_public_option_is_served()
    {
        var db = await SeedAsync();

        var option = await db.MenuCombos
            .Include(c => c.MenuTier!)
            .FirstAsync(c => c.MenuTier!.Key == "tier-08" && c.OptionCode == "C");

        var model = new ComboDetailsModel(new StubImageResolver(), NullLogger<ComboDetailsModel>.Instance, db);
        var result = await model.OnGetAsync(option.Id);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Combo);

        // The dish composition of the option is still rendered.
        Assert.Equal(7, model.Combo!.Dishes.Count);
    }

    [Fact]
    public async Task A_tierless_option_of_another_menu_set_stays_reachable()
    {
        var db = await SeedAsync();

        // A legacy demo row of a menu set that has no price tiers of its own.
        var demoMenuSet = new MenuSet
        {
            Name = "MENU DEMO",
            Slug = "menu-demo",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuSets.Add(demoMenuSet);
        await db.SaveChangesAsync();

        var demoCombo = new MenuCombo
        {
            MenuSetId = demoMenuSet.Id,
            Name = "COMBO DEMO 01",
            Slug = "combo-demo-01",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuCombos.Add(demoCombo);
        await db.SaveChangesAsync();

        var model = new ComboDetailsModel(new StubImageResolver(), NullLogger<ComboDetailsModel>.Instance, db);
        var result = await model.OnGetAsync(demoCombo.Id);

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task The_dish_page_does_not_link_to_an_excluded_option()
    {
        var db = await SeedAsync();
        var excluded = await AddExcludedOptionAsync(db);

        var dish = new Dish
        {
            Name = "Món của mức giá đã bị gỡ",
            Slug = "mon-cua-muc-gia-da-bi-go",
            SortOrder = 9999,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Dishes.Add(dish);
        await db.SaveChangesAsync();

        db.MenuComboDishes.Add(new MenuComboDish
        {
            MenuComboId = excluded.Id,
            DishId = dish.Id,
            SortOrder = 1,
            Quantity = 1
        });

        await db.SaveChangesAsync();

        var model = new DishDetailsModel(new StubImageResolver(), NullLogger<DishDetailsModel>.Instance, db);
        var result = await model.OnGetAsync(dish.Id);

        // The dish page itself still exists ...
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Dish);

        // ... but it does not link to the excluded option.
        Assert.Empty(model.Dish!.Combos);
    }

    [Fact]
    public async Task The_dish_page_links_to_the_public_options_of_a_dish()
    {
        var db = await SeedAsync();

        var dish = await db.Dishes.FirstAsync(d => d.Name == "Trái cây");

        var model = new DishDetailsModel(new StubImageResolver(), NullLogger<DishDetailsModel>.Instance, db);
        var result = await model.OnGetAsync(dish.Id);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Dish);

        // "Trái cây" is part of five public options.
        Assert.Equal(5, model.Dish!.Combos.Count);
    }

    [Fact]
    public void The_public_scope_only_knows_the_four_public_tiers()
    {
        Assert.Equal(4, PublicMenuScope.Tiers.Count);
        Assert.Equal(
            new[] { "tier-02", "tier-04", "tier-07", "tier-08" },
            PublicMenuScope.TierKeys.ToArray());

        Assert.True(PublicMenuScope.IsPublicTier("tier-08"));
        Assert.False(PublicMenuScope.IsPublicTier("tier-01"));

        Assert.Equal(
            new[] { "A", "B" },
            PublicMenuScope.OptionCodesFor("tier-02").ToArray());
        Assert.Equal(
            new[] { "A", "B", "C" },
            PublicMenuScope.OptionCodesFor("tier-08").ToArray());

        Assert.True(PublicMenuScope.IsPublicOption("tier-08", "C"));
        Assert.False(PublicMenuScope.IsPublicOption("tier-02", "C"));
        Assert.False(PublicMenuScope.IsPublicOption("tier-01", "A"));
    }

    [Fact]
    public void The_wedding_menu_set_slug_of_the_scope_matches_the_catalog()
    {
        var catalog = MenuCatalogSeeder.LoadCatalog();

        Assert.NotNull(catalog.MenuSet);
        Assert.Equal(PublicMenuScope.WeddingMenuSetSlug, catalog.MenuSet!.Slug);
    }

    private sealed class StubImageResolver : IDishImageUrlResolver
    {
        public string? Resolve(DishImage? image, int? width = null, int? height = null) =>
            image?.ImageUrl;
    }
}
