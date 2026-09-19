using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhiluWedding.Pages;

/// <summary>
/// Public wedding venue selection page. Renders two venue cards
/// (Golden Phoenix and Phì Lũ) and links each to its hall selection
/// route under <c>/tiec-cuoi/{slug}</c>. The list is hard-coded
/// because there are exactly two venues — no database lookup is
/// required.
/// </summary>
public class TiecCuoiModel : PageModel
{
    private readonly ILogger<TiecCuoiModel> _logger;

    public TiecCuoiModel(ILogger<TiecCuoiModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
