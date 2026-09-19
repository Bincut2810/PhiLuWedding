using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhiluWedding.Pages;

/// <summary>
/// Homepage. The page still keeps the editorial hero, introduction and
/// booking CTA. The previous "halls preview" 4-card section has been
/// replaced with a venue selection that lists the two wedding venues
/// (Golden Phoenix and Phì Lũ) — each card links to its venue-specific
/// hall selection page under <c>/tiec-cuoi/{venue-slug}</c>.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
