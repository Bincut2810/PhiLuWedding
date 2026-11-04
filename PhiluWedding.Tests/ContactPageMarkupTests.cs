using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhiluWedding.Data;
using Xunit;

namespace PhiluWedding.Tests;

/// <summary>
/// Integration tests for the public contact page (<c>/lien-he</c>).
/// They boot the application and check the HTML a visitor actually
/// receives: the two confirmed venue addresses, the click-to-call
/// hotline, the official Facebook and Zalo links, the Google Maps
/// destinations and the booking calls to action.
/// </summary>
public class ContactPageMarkupTests : IClassFixture<ContactWebApplicationFactory>
{
    private readonly ContactWebApplicationFactory _factory;

    public ContactPageMarkupTests(ContactWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> GetPageAsync(string url)
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync(url);
        return WebUtility.HtmlDecode(html);
    }

    [Fact]
    public async Task The_contact_page_is_served()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/lien-he");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_contact_page_shows_the_hero_and_the_booking_call_to_action()
    {
        var html = await GetPageAsync("/lien-he");

        Assert.Contains("Kết nối cùng", html);
        Assert.Contains("ĐẶT LỊCH TƯ VẤN", html);
        Assert.Contains("/dat-tiec", html);
    }

    [Fact]
    public async Task The_contact_page_shows_both_confirmed_venue_addresses()
    {
        var html = await GetPageAsync("/lien-he");

        Assert.Contains("Tiệc cưới Phì Lũ", html);
        Assert.Contains("Golden Phoenix", html);

        Assert.Contains(
            "3 Đ. 2 Tháng 9, Hòa Cường, Đà Nẵng 550000, Việt Nam",
            html);
        Assert.Contains(
            "A8.2 Lô 1, 2 Đ. 2 Tháng 9, Hòa Cường, Đà Nẵng 550000, Việt Nam",
            html);
    }

    [Fact]
    public async Task The_contact_page_links_to_both_google_maps_destinations()
    {
        var html = await GetPageAsync("/lien-he");

        // Two embedded maps, one per venue.
        Assert.Equal(2, CountOccurrences(html, "maps.google.com/maps?q="));

        // Two directions links per venue (the card and the map caption).
        Assert.Equal(4, CountOccurrences(html, "google.com/maps/dir/?api=1"));

        // The destinations are the confirmed addresses, not coordinates.
        Assert.Contains(
            "google.com/maps/dir/?api=1&destination=3%20%C4%90.%202%20Th%C3%A1ng%209",
            html);
        Assert.Contains(
            "google.com/maps/dir/?api=1&destination=A8.2%20L%C3%B4%201%2C%202%20%C4%90.%202%20Th%C3%A1ng%209",
            html);
    }

    [Fact]
    public async Task The_contact_page_offers_the_hotline_facebook_and_zalo()
    {
        var html = await GetPageAsync("/lien-he");

        // Click-to-call.
        Assert.Contains("tel:0905434115", html);
        Assert.Contains("0905434115", html);

        // The official social links, taken from the shared configuration.
        Assert.Contains("https://www.facebook.com/share/1BqiX3HRck/", html);
        Assert.Contains("https://zalo.me/0989821022", html);
    }

    [Fact]
    public async Task The_contact_page_has_no_placeholder_content()
    {
        var html = await GetPageAsync("/lien-he");

        Assert.DoesNotContain("Địa chỉ: (cập nhật sau)", html);
        Assert.DoesNotContain("đang cập nhật", html);
    }

    [Fact]
    public async Task The_floating_contact_bubbles_are_preserved_on_the_contact_page()
    {
        var html = await GetPageAsync("/lien-he");

        Assert.Contains("contact-bubbles__link--hotline", html);
        Assert.Contains("contact-bubbles__link--facebook", html);
        Assert.Contains("contact-bubbles__link--zalo", html);
    }

    private static int CountOccurrences(string haystack, string needle) =>
        haystack.Split(needle).Length - 1;
}

/// <summary>
/// Boots the application with the EF Core in-memory provider. The
/// contact page does not read from the database, so no seeding is
/// needed here.
/// </summary>
public class ContactWebApplicationFactory : WebApplicationFactory<Program>
{
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
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        });
    }
}
