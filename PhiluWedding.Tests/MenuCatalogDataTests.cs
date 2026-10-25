using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Data-integrity tests for the fixed wedding-menu catalog: eight price
/// tiers, seventeen options, ordered dishes and the additional menu
/// sections.
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
    public async Task Exactly_eight_price_tiers_exist()
    {
        var db = await SeedAsync();
        var tiers = await db.MenuTiers.OrderBy(t => t.SortOrder).ToListAsync();

        Assert.Equal(8, tiers.Count);
        Assert.Equal(
            new[] { 2200000m, 2500000m, 2800000m, 3000000m, 3300000m, 3500000m, 4000000m, 5000000m },
            tiers.Select(t => t.Price).ToArray());
        Assert.All(tiers, t => Assert.Equal(10, t.GuestsPerTable));
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
    public async Task There_are_seventeen_options_with_the_expected_codes()
    {
        var db = await SeedAsync();
        var options = await db.MenuCombos
            .Where(c => c.OptionCode != null)
            .Include(c => c.MenuTier!)
            .ToListAsync();

        Assert.Equal(17, options.Count);
        Assert.Equal(8, options.Count(o => o.OptionCode == "A"));
        Assert.Equal(8, options.Count(o => o.OptionCode == "B"));
        Assert.Equal(1, options.Count(o => o.OptionCode == "C"));

        // Every option belongs to exactly one tier.
        Assert.All(options, o => Assert.NotNull(o.MenuTier));
        Assert.Equal(8, options.Select(o => o.MenuTierId).Distinct().Count());
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

        // The other two options of the same tier resolve to the same price.
        var tierEightOptions = await db.MenuCombos
            .Where(c => c.MenuTierId == optionC.MenuTierId)
            .Include(c => c.MenuTier!)
            .ToListAsync();

        Assert.Equal(3, tierEightOptions.Count);
        Assert.All(tierEightOptions, o => Assert.Equal(5000000m, o.MenuTier!.Price));
        Assert.All(tierEightOptions, o => Assert.Null(o.Price));
    }

    [Fact]
    public async Task No_ninth_price_tier_exists()
    {
        var db = await SeedAsync();

        // "Option C" must never have been promoted into its own price tier.
        Assert.Equal(8, await db.MenuTiers.CountAsync());
        Assert.DoesNotContain(await db.MenuTiers.ToListAsync(), t => t.Price == 0m);
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
            }
        }
    }

    [Fact]
    public async Task The_same_dish_may_appear_in_several_options_without_duplicate_rows()
    {
        var db = await SeedAsync();

        // "Rau câu" appears in tier 1 A, tier 1 B, tier 3 A, tier 3 B and tier 6 A.
        var rauCau = await db.Dishes
            .Include(d => d.ComboDishes)
            .FirstAsync(d => d.Name == "Rau câu");

        var comboIds = rauCau.ComboDishes.Select(l => l.MenuComboId).ToList();

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
        Assert.Equal(1, await db.Dishes.CountAsync(d => d.Name == "Rau câu"));
    }

    [Fact]
    public async Task A_dish_written_with_two_capitalisations_is_stored_once()
    {
        var db = await SeedAsync();

        // The catalog writes "Khai vị Tứ quý" (tier 5) and "Khai vị Tứ Quý"
        // (tiers 6 and 8). It is one dish, so the catalog keeps a single row
        // and both options reference it.
        var matches = await db.Dishes
            .Where(d => d.Name.StartsWith("Khai vị Tứ"))
            .ToListAsync();

        var dish = Assert.Single(matches);

        var comboIds = await db.MenuComboDishes
            .Where(l => l.DishId == dish.Id)
            .Select(l => l.MenuComboId)
            .ToListAsync();

        // Three options across three different tiers share the one row.
        Assert.Equal(3, comboIds.Distinct().Count());
    }

    [Fact]
    public async Task Vietnamese_text_survives_seeding_and_retrieval()
    {
        var db = await SeedAsync();

        var names = await db.Dishes.Select(d => d.Name).ToListAsync();

        Assert.Contains("Gỏi Phượng Hoàng + bpt", names);
        Assert.Contains("Sườn non nấu lagu + bánh mỳ", names);
        Assert.Contains("Cá hồi nướng phomai kèm rau củ", names);
        Assert.Contains("Tôm sú nướng sốt Wasabi", names);
        Assert.Contains("Bò nấu kiểu Ý + bánh mỳ", names);
        Assert.Contains("Khai vị Tứ quý", names);
        Assert.Contains("Nem, chả, gỏi thập cẩm + bpt", names);

        // Slugs stay ASCII, so URLs do not carry raw diacritics.
        var slug = await db.Dishes.Where(d => d.Name == "Gỏi Phượng Hoàng + bpt")
            .Select(d => d.Slug)
            .SingleAsync();
        Assert.Equal("goi-phuong-hoang-bpt", slug);
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
    public async Task Service_entries_preserve_their_price_semantics()
    {
        var db = await SeedAsync();
        var services = await db.MenuServices.OrderBy(s => s.SortOrder).ToListAsync();

        Assert.Equal(17, services.Count);

        // A plain fixed amount.
        var dance4 = services.Single(s => s.Name == "Bài múa hoặc bài nhảy (4 diễn viên)");
        Assert.Equal(MenuPriceTypes.Fixed, dance4.PriceType);
        Assert.Equal(1500000m, dance4.PriceFrom);
        Assert.Null(dance4.PriceTo);

        // "từ ..." — a starting amount, not an exact price.
        var singer = services.Single(s => s.Name == "Ca sỹ nhạc nhẹ (tùy ca sỹ)");
        Assert.Equal(MenuPriceTypes.From, singer.PriceType);
        Assert.Equal(2000000m, singer.PriceFrom);
        Assert.Null(singer.PriceTo);

        // A price that depends on the chosen type — no number is invented.
        var gifts = services.Single(s => s.Name == "Quà tặng dành cho khách dự tiệc");
        Assert.Equal(MenuPriceTypes.DependsOnType, gifts.PriceType);
        Assert.Null(gifts.PriceFrom);
        Assert.Null(gifts.PriceTo);

        // A range keeps both ends.
        var cleaning = services.Single(s => s.Name.Contains("Phí vệ sinh"));
        Assert.Equal(MenuPriceTypes.Range, cleaning.PriceType);
        Assert.Equal(1000000m, cleaning.PriceFrom);
        Assert.Equal(3000000m, cleaning.PriceTo);
        Assert.Contains("tư vấn", cleaning.Note);

        // Per-table units are kept as a unit, not baked into the amount.
        var vip = services.Single(s => s.Name == "Phong cách phục vụ VIP");
        Assert.Equal(200000m, vip.PriceFrom);
        Assert.Equal("bàn", vip.Unit);

        var bringDrinks = services.Single(s => s.Name == "Khách hàng mang nước uống đến");
        Assert.Equal(150000m, bringDrinks.PriceFrom);
        Assert.Equal("bàn", bringDrinks.Unit);

        var bringWine = services.Single(s => s.Name == "Phí mang rượu vào");
        Assert.Equal(300000m, bringWine.PriceFrom);
        Assert.Equal("bàn", bringWine.Unit);
    }

    [Fact]
    public async Task All_thirteen_beverages_keep_both_tier_prices()
    {
        var db = await SeedAsync();
        var beverages = await db.MenuBeverages.OrderBy(b => b.SortOrder).ToListAsync();

        Assert.Equal(13, beverages.Count);
        Assert.All(beverages, b => Assert.NotNull(b.PriceBelowThreshold));
        Assert.All(beverages, b => Assert.NotNull(b.PriceAboveThreshold));

        // The two prices are different for every beverage in the source sheet.
        Assert.All(beverages, b => Assert.True(b.PriceBelowThreshold > b.PriceAboveThreshold));

        Assert.Equal(17500m, beverages[0].PriceBelowThreshold);
        Assert.Equal(16500m, beverages[0].PriceAboveThreshold);
        Assert.Equal(25500m, beverages[2].PriceBelowThreshold);
        Assert.Equal(24500m, beverages[2].PriceAboveThreshold);
        Assert.Equal(10000m, beverages[11].PriceBelowThreshold);
        Assert.Equal(9000m, beverages[11].PriceAboveThreshold);

        // The threshold keeps the source wording; exactly 25 tables is not
        // specified by the source and must not be invented.
        Assert.All(beverages, b => Assert.Equal(25, b.ThresholdTableCount));
        Assert.All(beverages, b => Assert.Contains("25", b.ThresholdNote));
    }

    [Fact]
    public async Task All_six_appetizer_nuts_exist_without_invented_prices()
    {
        var db = await SeedAsync();
        var nuts = await db.MenuAppetizerNuts.OrderBy(n => n.SortOrder).ToListAsync();

        Assert.Equal(6, nuts.Count);

        // Two entries follow the market price.
        foreach (var marketPriced in nuts.Where(n => n.PriceType == MenuPriceTypes.TimeDependent))
        {
            Assert.Null(marketPriced.Price);
            Assert.NotNull(marketPriced.Note);
        }

        // The other four have no price stated at all.
        foreach (var unspecified in nuts.Where(n => n.PriceType == MenuPriceTypes.Unspecified))
        {
            Assert.Null(unspecified.Price);
        }

        Assert.Equal(2, nuts.Count(n => n.PriceType == MenuPriceTypes.TimeDependent));
        Assert.Equal(4, nuts.Count(n => n.PriceType == MenuPriceTypes.Unspecified));

        Assert.Contains(nuts, n => n.Name == "Hạt dưa");
        Assert.Contains(nuts, n => n.Name == "Hạt điều");
        Assert.Contains(nuts, n => n.Name == "Hạt đậu nành sấy");
        Assert.Contains(nuts, n => n.Name == "Hạt hướng dương");
        Assert.Contains(nuts, n => n.Name == "Đậu phộng rang muối");
        Assert.Contains(nuts, n => n.Name == "Đậu phộng da cá rim tỏi ớt");
    }

    [Fact]
    public async Task All_eight_promotion_tiers_and_the_shared_terms_are_represented()
    {
        var db = await SeedAsync();
        var promotions = await db.MenuPromotions.OrderBy(p => p.SortOrder).ToListAsync();

        Assert.Equal(8, promotions.Count);
        Assert.Equal(14, promotions[0].MinTableCount);
        Assert.Equal(24, promotions[0].MaxTableCount);
        Assert.Equal(96, promotions[7].MinTableCount);
        Assert.Null(promotions[7].MaxTableCount);

        // The inheritance chain makes the cumulative benefit list
        // reconstructable without duplicating the inherited benefits.
        Assert.Null(promotions[0].BasePromotionId);
        for (var i = 1; i < promotions.Count; i++)
        {
            Assert.Equal(promotions[i - 1].Id, promotions[i].BasePromotionId);
            Assert.True(promotions[i].IncludesPreviousTier);
        }

        // Every tier stores only the benefits it adds.
        Assert.Equal(3, await db.MenuPromotionBenefits.CountAsync(b => b.MenuPromotionId == promotions[0].Id));
        Assert.Equal(10, await db.MenuPromotionBenefits.CountAsync(b => b.MenuPromotionId == promotions[1].Id));

        // The cumulative list for the top tier is deterministic.
        var cumulative = BuildCumulativeBenefits(promotions[7]);
        Assert.Equal(28, cumulative.Count);
        Assert.Equal("Bông trang trí bàn tiệc", cumulative[0]);
        Assert.Equal("Miễn phí 2 bàn dùng sau tiệc (không trừ vào số bàn đã đặt)", cumulative[^1]);

        var terms = await db.MenuPromotionTerms.OrderBy(t => t.SortOrder).ToListAsync();
        Assert.Equal(3, terms.Count);
        Assert.All(terms, t => Assert.False(string.IsNullOrWhiteSpace(t.Content)));
    }

    [Fact]
    public async Task Deposit_terms_belong_to_the_correct_philu_halls_only()
    {
        var db = await SeedAsync();
        var terms = await db.MenuDepositTerms.OrderBy(t => t.SortOrder).ToListAsync();

        // Four halls, two deposit stages, plus one venue-wide final payment.
        Assert.Equal(9, terms.Count);

        // No Golden Phoenix deposit terms exist at all.
        Assert.DoesNotContain(terms, t => t.VenueScope != MenuVenueScopes.PhiLu);
        Assert.DoesNotContain(terms, t => t.HallSlug != null && t.HallSlug.Contains("golden"));

        var expected = new Dictionary<string, (decimal First, decimal Second)>
        {
            ["philu-sanh-amber"] = (12000000m, 18000000m),
            ["philu-sanh-pearl"] = (8000000m, 12000000m),
            ["philu-sanh-crystal"] = (5000000m, 5000000m),
            ["philu-sanh-diamond"] = (5000000m, 5000000m)
        };

        foreach (var (hallSlug, amounts) in expected)
        {
            var first = terms.Single(t => t.HallSlug == hallSlug && t.Stage == DepositStage.First);
            var second = terms.Single(t => t.HallSlug == hallSlug && t.Stage == DepositStage.Second);

            Assert.Equal(amounts.First, first.Amount);
            Assert.Equal(amounts.Second, second.Amount);
            Assert.Equal(MenuVenueScopes.PhiLu, first.VenueScope);
        }

        // The final payment is a venue-wide policy with no amount.
        var final = terms.Single(t => t.Stage == DepositStage.Final);
        Assert.Null(final.HallSlug);
        Assert.Null(final.Amount);
        Assert.Contains("sau khi kết thúc tiệc", final.Description);
    }

    [Fact]
    public async Task Deposit_terms_link_to_the_hall_rows_when_they_exist()
    {
        var db = await SeedAsync();

        db.WeddingHalls.Add(new WeddingHall
        {
            Name = "Sảnh Amber",
            Slug = "philu-sanh-amber",
            VenueKey = VenueKeys.PhiLu,
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.WeddingHalls.Add(new WeddingHall
        {
            Name = "Sảnh A",
            Slug = "golden-phoenix-sanh-a",
            VenueKey = VenueKeys.GoldenPhoenix,
            SortOrder = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        // Re-running the seed links the term to the hall that now exists.
        await MenuCatalogSeeder.SeedAsync(db, TestDbContextFactory.Logger);

        var amberTerms = await db.MenuDepositTerms
            .Where(t => t.HallSlug == "philu-sanh-amber")
            .ToListAsync();

        Assert.Equal(2, amberTerms.Count);
        Assert.All(amberTerms, t => Assert.NotNull(t.WeddingHallId));

        // The Golden Phoenix hall is never picked up by a Phì Lũ term.
        Assert.All(amberTerms, t => Assert.NotEqual(MenuVenueScopes.GoldenPhoenix, t.VenueScope));
    }

    [Fact]
    public async Task The_price_validity_note_states_the_original_period_and_the_vat_exclusion()
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
    public async Task Non_shared_sections_stay_scoped_to_the_venue_they_come_from()
    {
        var db = await SeedAsync();

        // The price tiers and the menu set are shared by both venues.
        Assert.All(await db.MenuTiers.ToListAsync(), t => Assert.Equal(MenuVenueScopes.BothVenues, t.VenueScope));

        var menuSet = await db.MenuSets.SingleAsync(s => s.Slug == "menu-tiec-cuoi");
        Assert.Equal(MenuVenueScopes.BothVenues, menuSet.VenueScope);

        // Everything else comes from the Phì Lũ source sheet and is not
        // confirmed for Golden Phoenix.
        Assert.All(await db.MenuServices.ToListAsync(), s => Assert.Equal(MenuVenueScopes.PhiLu, s.VenueScope));
        Assert.All(await db.MenuBeverages.ToListAsync(), b => Assert.Equal(MenuVenueScopes.PhiLu, b.VenueScope));
        Assert.All(await db.MenuAppetizerNuts.ToListAsync(), n => Assert.Equal(MenuVenueScopes.PhiLu, n.VenueScope));
        Assert.All(await db.MenuPromotions.ToListAsync(), p => Assert.Equal(MenuVenueScopes.PhiLu, p.VenueScope));
        Assert.All(await db.MenuDepositTerms.ToListAsync(), t => Assert.Equal(MenuVenueScopes.PhiLu, t.VenueScope));
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

    /// <summary>
    /// Rebuilds the full cumulative benefit list of a promotion tier by
    /// walking the inheritance chain from the bottom tier upwards.
    /// </summary>
    private static List<string> BuildCumulativeBenefits(MenuPromotion promotion)
    {
        var chain = new List<MenuPromotion>();
        var current = promotion;

        while (current is not null)
        {
            chain.Add(current);
            current = current.BasePromotion!;
        }

        chain.Reverse();

        return chain
            .SelectMany(p => p.Benefits.OrderBy(b => b.SortOrder).Select(b => b.Text))
            .ToList();
    }
}
