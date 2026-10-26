using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhiluWedding.Data;
using PhiluWedding.Pages.ThucDon;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Tests for the two public wedding-menu pages
/// (<c>/thuc-don/philu</c> and <c>/thuc-don/golden-phoenix</c>): the shared
/// price tiers, the public menu options and the fallback of a price tier
/// that is not public.
/// </summary>
public class MenuVenuePageTests
{
    private static async Task<PhiluWeddingDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);
        return db;
    }

    private static MenuPhiLuModel PhiLu(PhiluWeddingDbContext db) =>
        new(NullLogger<MenuPhiLuModel>.Instance, db);

    private static MenuGoldenPhoenixModel GoldenPhoenix(PhiluWeddingDbContext db) =>
        new(NullLogger<MenuGoldenPhoenixModel>.Instance, db);

    [Fact]
    public async Task Both_venues_show_the_same_four_price_tiers_with_the_same_prices()
    {
        var db = await SeedAsync();

        var phiLu = PhiLu(db);
        await phiLu.OnGetAsync(null);

        var goldenPhoenix = GoldenPhoenix(db);
        await goldenPhoenix.OnGetAsync(null);

        Assert.Equal(4, phiLu.Tiers.Count);
        Assert.Equal(4, goldenPhoenix.Tiers.Count);

        Assert.Equal(
            phiLu.Tiers.Select(t => t.Price),
            goldenPhoenix.Tiers.Select(t => t.Price));

        Assert.Equal(
            new[] { "2.500.000đ", "3.000.000đ", "4.000.000đ", "5.000.000đ" },
            phiLu.Tiers.Select(t => t.PriceDisplay));

        // Every tier is priced identically for both venues.
        Assert.All(phiLu.Tiers, t => Assert.True(t.IsSharedPrice));
    }

    [Fact]
    public async Task The_nine_public_options_are_shown_across_the_four_tiers()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        var options = model.Tiers.SelectMany(t => t.Options).ToList();

        Assert.Equal(9, options.Count);
        Assert.Equal(4, options.Count(o => o.Code == "A"));
        Assert.Equal(4, options.Count(o => o.Code == "B"));
        Assert.Equal(1, options.Count(o => o.Code == "C"));

        // The first three tiers sell A and B, the 5.000.000 VND tier sells
        // A, B and C.
        foreach (var tier in model.Tiers.Take(3))
        {
            Assert.Equal(new[] { "A", "B" }, tier.Options.Select(o => o.Code).ToArray());
        }

        var lastTier = model.Tiers[^1];
        Assert.Equal("menu-tiec-cuoi-tier-08", lastTier.Slug);
        Assert.Equal(new[] { "A", "B", "C" }, lastTier.Options.Select(o => o.Code).ToArray());
    }

    [Fact]
    public async Task An_option_shows_its_name_without_the_price_tier_prefix()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync("menu-tiec-cuoi-tier-08");

        var tier = model.SelectedTier;
        Assert.NotNull(tier);
        Assert.Equal("menu-tiec-cuoi-tier-08", tier!.Slug);
        Assert.Equal(3, tier.Options.Count);

        Assert.Equal(
            new[] { "Phương án A", "Phương án B", "Phương án C" },
            tier.Options.Select(o => o.ShortName));
    }

    [Fact]
    public async Task An_unknown_tier_selector_falls_back_to_the_first_tier()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync("khong-ton-tai");

        Assert.NotNull(model.SelectedTier);
        Assert.Equal("menu-tiec-cuoi-tier-02", model.SelectedTier!.Slug);
    }

    [Fact]
    public async Task The_selector_of_an_excluded_tier_falls_back_to_the_first_tier()
    {
        var db = await SeedAsync();

        // The tiers that are no longer part of the public menu are not
        // selectable, so their slug falls back to the first public tier.
        foreach (var slug in new[]
        {
            "menu-tiec-cuoi-tier-01",
            "menu-tiec-cuoi-tier-03",
            "menu-tiec-cuoi-tier-05",
            "menu-tiec-cuoi-tier-06"
        })
        {
            var model = PhiLu(db);
            await model.OnGetAsync(slug);

            Assert.NotNull(model.SelectedTier);
            Assert.Equal("menu-tiec-cuoi-tier-02", model.SelectedTier!.Slug);
        }
    }

    [Fact]
    public async Task An_unpublished_tier_is_not_shown()
    {
        var db = await SeedAsync();

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-04");
        tier.IsPublished = false;
        await db.SaveChangesAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        Assert.Equal(3, model.Tiers.Count);
        Assert.DoesNotContain(model.Tiers, t => t.Key == "tier-04");
    }

    [Fact]
    public async Task An_unpublished_option_is_not_shown()
    {
        var db = await SeedAsync();

        var tier = await db.MenuTiers.FirstAsync(t => t.Key == "tier-08");
        var option = await db.MenuCombos
            .FirstAsync(c => c.MenuTierId == tier.Id && c.OptionCode == "C");

        option.IsPublished = false;
        await db.SaveChangesAsync();

        var model = PhiLu(db);
        await model.OnGetAsync("menu-tiec-cuoi-tier-08");

        var selected = model.SelectedTier;
        Assert.NotNull(selected);
        Assert.Equal(new[] { "A", "B" }, selected!.Options.Select(o => o.Code).ToArray());
    }

    [Fact]
    public async Task The_price_notes_are_still_shown()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        Assert.Equal(2, model.PriceNotes.Count);

        var note = model.PriceNotes.First();
        Assert.Equal("Giá tham khảo", note.Title);
        Assert.Equal("31/08/2026", note.ValidUntilDisplay);
        Assert.Equal("Giá chưa bao gồm VAT (theo bảng giá gốc).", note.VatNote);
    }
}
