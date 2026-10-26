using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Integration tests for the public wedding-menu pages. They boot the
/// application with the EF Core in-memory provider and check the HTML that
/// a visitor actually receives.
/// </summary>
public class MenuVenueMarkupTests : IClassFixture<MenuWebApplicationFactory>
{
    private readonly MenuWebApplicationFactory _factory;
    private readonly Task _seeded;

    public MenuVenueMarkupTests(MenuWebApplicationFactory factory)
    {
        _factory = factory;
        _seeded = factory.SeedAsync();
    }

    private async Task<string> GetPageAsync(string url)
    {
        await _seeded;

        var client = _factory.CreateClient();

        return WebUtility.HtmlDecode(await client.GetStringAsync(url));
    }

    private async Task<string> GetPageWithTierAsync(string tierSlug)
    {
        await _seeded;

        var client = _factory.CreateClient();

        var html = await client.GetStringAsync($"/thuc-don/philu?muc={tierSlug}");

        return WebUtility.HtmlDecode(html);
    }

    [Fact]
    public async Task The_venue_page_shows_the_four_public_price_tiers()
    {
        var html = await GetPageAsync("/thuc-don/philu");

        Assert.Contains("2.500.000", html);
        Assert.Contains("3.000.000", html);
        Assert.Contains("4.000.000", html);
        Assert.Contains("5.000.000", html);

        // The removed price levels are gone.
        Assert.DoesNotContain("2.200.000", html);
        Assert.DoesNotContain("2.800.000", html);
        Assert.DoesNotContain("3.300.000", html);
        Assert.DoesNotContain("3.500.000", html);
    }

    [Fact]
    public async Task The_venue_page_has_no_removed_section_and_no_jump_link_to_it()
    {
        var html = await GetPageAsync("/thuc-don/philu");

        // The jump-navigation links of the removed sections.
        Assert.DoesNotContain("#menu-services", html);
        Assert.DoesNotContain("#menu-beverages", html);
        Assert.DoesNotContain("#menu-nuts", html);
        Assert.DoesNotContain("#menu-promotions", html);
        Assert.DoesNotContain("#menu-deposit", html);

        // The sections themselves.
        Assert.DoesNotContain("menu-services", html);
        Assert.DoesNotContain("menu-beverages", html);
        Assert.DoesNotContain("menu-nuts", html);
        Assert.DoesNotContain("menu-promotions", html);
        Assert.DoesNotContain("menu-deposit", html);

        Assert.DoesNotContain("Dịch vụ và phí phụ", html);
        Assert.DoesNotContain("Hạt ăn khai", html);
        Assert.DoesNotContain("Quyền lợi theo số bàn", html);
        Assert.DoesNotContain("Điều khoản đặt cọc", html);
        Assert.DoesNotContain("Bảng giá nước uống", html);
    }

    [Fact]
    public async Task The_venue_page_still_shows_the_price_notes_and_the_booking_call_to_action()
    {
        var html = await GetPageAsync("/thuc-don/philu");

        Assert.Contains("menu-prices", html);
        Assert.Contains("menu-options", html);
        Assert.Contains("menu-notes", html);
        Assert.Contains("Ghi chú về giá", html);
        Assert.Contains("Giá tham khảo", html);
        Assert.Contains("ĐẶT LỊCH TƯ VẤN", html);
    }

    [Fact]
    public async Task The_five_million_tier_shows_the_options_a_b_and_c()
    {
        var html = await GetPageWithTierAsync("menu-tiec-cuoi-tier-08");

        Assert.Contains("Phương án A", html);
        Assert.Contains("Phương án B", html);
        Assert.Contains("Phương án C", html);
    }

    [Fact]
    public async Task The_two_million_five_hundred_tier_shows_the_options_a_and_b_only()
    {
        var html = await GetPageWithTierAsync("menu-tiec-cuoi-tier-02");

        Assert.Contains("Phương án A", html);
        Assert.Contains("Phương án B", html);
        Assert.DoesNotContain("Phương án C", html);
    }

    [Fact]
    public async Task The_selector_of_an_excluded_tier_falls_back_to_the_first_public_tier()
    {
        await _seeded;

        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/thuc-don/philu?muc=menu-tiec-cuoi-tier-01");

        // The first public tier is the selected one.
        Assert.Contains("aria-current=\"true\"", html);
        Assert.Contains("muc=menu-tiec-cuoi-tier-02", html);
        Assert.DoesNotContain("muc=menu-tiec-cuoi-tier-01\"", html);
    }

    [Fact]
    public async Task The_same_four_tiers_are_shown_for_the_second_venue()
    {
        await _seeded;

        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/thuc-don/golden-phoenix");

        Assert.Contains("2.500.000", html);
        Assert.Contains("5.000.000", html);
        Assert.DoesNotContain("2.200.000", html);
    }

    [Fact]
    public async Task A_request_for_an_excluded_option_is_not_found()
    {
        await _seeded;

        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/thuc-don/combo/{_factory.ExcludedOptionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_request_for_a_public_option_is_served()
    {
        await _seeded;

        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/thuc-don/combo/{_factory.PublicOptionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Phương án C", html);
    }
}

/// <summary>
/// Boots the application with the EF Core in-memory provider and a seeded
/// menu catalog. The excluded price tier and its option are added as well,
/// the way they still exist in a database that was seeded before the
/// catalog was trimmed.
/// </summary>
public class MenuWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    /// <summary>The id of a published option of the excluded price tier.</summary>
    public int ExcludedOptionId { get; private set; }

    /// <summary>The id of the option C of the 5.000.000 VND price tier.</summary>
    public int PublicOptionId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // The application must not use the connection string of the
        // configuration file while the tests run.
        builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<PhiluWeddingDbContext>>();
            services.RemoveAll<PhiluWeddingDbContext>();

            services.AddDbContext<PhiluWeddingDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Seeds the catalog the public pages read.</summary>
    public Task SeedAsync() => _seedTask ??= SeedCoreAsync();

    private Task? _seedTask;

    private async Task SeedCoreAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhiluWeddingDbContext>();

        await MenuCatalogSeeder.SeedAsync(db, NullLogger.Instance);

        var menuSet = await db.MenuSets.FirstAsync(s => s.Slug == PublicMenuScope.WeddingMenuSetSlug);

        var excludedTier = new MenuTier
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

        db.MenuTiers.Add(excludedTier);
        await db.SaveChangesAsync();

        var excludedOption = new MenuCombo
        {
            MenuSetId = menuSet.Id,
            MenuTierId = excludedTier.Id,
            OptionCode = "A",
            OptionSortOrder = 10,
            Name = "Thực đơn 2.200.000 đồng/bàn - Phương án A",
            Slug = "menu-tiec-cuoi-tier-01-a",
            SortOrder = 510,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MenuCombos.Add(excludedOption);
        await db.SaveChangesAsync();

        ExcludedOptionId = excludedOption.Id;

        var publicOption = await db.MenuCombos
            .Include(c => c.MenuTier!)
            .FirstAsync(c => c.MenuTier!.Key == "tier-08" && c.OptionCode == "C");

        PublicOptionId = publicOption.Id;
    }
}
