using Microsoft.Extensions.Logging.Abstractions;
using PhiluWedding.Data;
using PhiluWedding.Pages.ThucDon;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Tests for the two public wedding-menu pages
/// (<c>/thuc-don/philu</c> and <c>/thuc-don/golden-phoenix</c>): the shared
/// price tiers, the venue scope of the supporting sections and the wording
/// the pages show.
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
    public async Task Both_venues_show_the_same_eight_price_tiers_with_the_same_prices()
    {
        var db = await SeedAsync();

        var phiLu = PhiLu(db);
        await phiLu.OnGetAsync(null);

        var goldenPhoenix = GoldenPhoenix(db);
        await goldenPhoenix.OnGetAsync(null);

        Assert.Equal(8, phiLu.Tiers.Count);
        Assert.Equal(8, goldenPhoenix.Tiers.Count);

        Assert.Equal(
            phiLu.Tiers.Select(t => t.Price),
            goldenPhoenix.Tiers.Select(t => t.Price));

        Assert.Equal(
            new[] { "2.200.000đ", "2.500.000đ", "2.800.000đ", "3.000.000đ", "3.300.000đ", "3.500.000đ", "4.000.000đ", "5.000.000đ" },
            phiLu.Tiers.Select(t => t.PriceDisplay));

        // Every tier is priced identically for both venues.
        Assert.All(phiLu.Tiers, t => Assert.True(t.IsSharedPrice));
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
        Assert.Equal("menu-tiec-cuoi-tier-01", model.SelectedTier!.Slug);
    }

    [Fact]
    public async Task The_beverage_unit_is_only_shown_where_the_source_states_one()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        var laru = model.Beverages.First(b => b.Name == "Laru lon vàng");
        Assert.Equal("17.500đ / lon", laru.PriceBelowDisplay);
        Assert.Equal("16.500đ / lon", laru.PriceAboveDisplay);

        var laruSmooth = model.Beverages.First(b => b.Name == "Laru smooth");
        Assert.Equal("17.500đ", laruSmooth.PriceBelowDisplay);
        Assert.Equal("16.500đ", laruSmooth.PriceAboveDisplay);

        // The threshold and its wording come from the source sheet.
        Assert.Equal(25, model.BeverageThresholdTableCount);
        Assert.Contains("25 bàn", model.BeverageThresholdNote);
    }

    [Fact]
    public async Task The_supporting_sections_are_scoped_to_the_venue_they_belong_to()
    {
        var db = await SeedAsync();

        var phiLu = PhiLu(db);
        await phiLu.OnGetAsync(null);

        Assert.True(phiLu.HasServices);
        Assert.True(phiLu.HasBeverages);
        Assert.True(phiLu.HasNuts);
        Assert.True(phiLu.HasPromotions);
        Assert.True(phiLu.HasDepositTerms);
        Assert.False(phiLu.HasNoSupportingSections);

        var goldenPhoenix = GoldenPhoenix(db);
        await goldenPhoenix.OnGetAsync(null);

        Assert.False(goldenPhoenix.HasServices);
        Assert.False(goldenPhoenix.HasBeverages);
        Assert.False(goldenPhoenix.HasNuts);
        Assert.False(goldenPhoenix.HasPromotions);
        Assert.False(goldenPhoenix.HasDepositTerms);
        Assert.True(goldenPhoenix.HasNoSupportingSections);

        // The price tiers are still shown on both pages.
        Assert.Equal(8, goldenPhoenix.Tiers.Count);
    }

    [Fact]
    public async Task A_service_without_a_price_shows_the_source_wording()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        var gifts = model.Services.First(s => s.Name == "Quà tặng dành cho khách dự tiệc");
        Assert.Null(gifts.PriceDisplay);
        Assert.Equal("Giá tùy thuộc vào loại quà tặng.", gifts.PriceFallback);

        // A service with a price keeps the unit of the source sheet.
        var vip = model.Services.First(s => s.Name == "Phong cách phục vụ VIP");
        Assert.Equal("200.000đ / bàn", vip.PriceDisplay);
    }

    [Fact]
    public async Task The_promotion_benefits_of_a_tier_include_the_inherited_ones()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        var first = model.Promotions.First(p => p.RangeLabel == "14–24 bàn");
        Assert.False(first.IncludesPreviousTier);
        Assert.Equal(3, first.CumulativeBenefits.Count);

        var second = model.Promotions.First(p => p.RangeLabel == "25–35 bàn");
        Assert.True(second.IncludesPreviousTier);
        Assert.Equal("14–24 bàn", second.BaseRangeLabel);
        Assert.Equal(3 + 10, second.CumulativeBenefits.Count);
    }

    [Fact]
    public async Task The_deposit_terms_are_grouped_per_hall()
    {
        var db = await SeedAsync();

        var model = PhiLu(db);
        await model.OnGetAsync(null);

        Assert.Equal(4, model.DepositHalls.Count);

        var amber = model.DepositHalls.First(h => h.HallName == "Sảnh Amber");
        Assert.Equal("12.000.000đ", amber.First!.AmountDisplay);
        Assert.Equal("18.000.000đ", amber.Second!.AmountDisplay);
        Assert.Equal("Đặt cọc giữ chỗ.", amber.First.Description);

        // The final payment applies to the whole venue and carries no amount.
        Assert.Single(model.VenueWideDepositStages);
        Assert.Null(model.VenueWideDepositStages[0].AmountDisplay);
    }

    [Fact]
    public async Task A_price_note_keeps_its_validity_and_vat_wording()
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
