using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using PhiluWedding.Services;

namespace PhiluWedding.Pages;

/// <summary>
/// The public contact page (<c>/lien-he</c>).
///
/// <para>
/// The page is intentionally free of database access: every piece of
/// information on it is confirmed business truth that is already known
/// to the application — the two venue addresses, the hotline and the
/// official social links. That keeps the page fast, always available
/// (even when PostgreSQL is unavailable) and impossible to break by a
/// stale row.
/// </para>
/// </summary>
public class LienHeModel : PageModel
{
    private readonly ILogger<LienHeModel> _logger;

    public LienHeModel(ILogger<LienHeModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        Contact = BrandContact.Resolve(configuration);
    }

    /// <summary>The official contact channels (hotline, Facebook, Zalo).</summary>
    public BrandContactInfo Contact { get; }

    /// <summary>The two venues with their confirmed addresses and map links.</summary>
    public IReadOnlyList<VenueContact> Venues => VenueContactCatalog.Venues;

    public void OnGet()
    {
        _logger.LogInformation("Contact page requested.");
    }
}
