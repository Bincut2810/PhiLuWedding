using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Public detail page for a single <see cref="WeddingHall"/>. Route is
/// <c>/tiec-cuoi/{id:int}</c>. Data comes from the database.
///
/// <para>
/// The companion slug-based route
/// <c>/tiec-cuoi/hall/{slug}</c> lives in <see cref="HallModel"/> and
/// renders the same template (via the shared
/// <c>Pages/TiecCuoi/_HallDetail.cshtml</c> partial) using the
/// <see cref="StaticVenueCatalog"/>.
/// </para>
/// </summary>
public class DetailsModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<DetailsModel> _logger;

    public HallViewModel? Hall { get; private set; }

    /// <summary>Back-target URL for the breadcrumb / "Quay lại" link.</summary>
    public string BackHref { get; private set; } = "/TiecCuoi";

    public DetailsModel(
        IHallImageUrlResolver imageUrl,
        ILogger<DetailsModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        _db = db;
        _imageUrl = imageUrl;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (_db is null)
        {
            return NotFound();
        }

        try
        {
            var hall = await _db.WeddingHalls
                .AsNoTracking()
                .Where(h => h.Id == id && h.IsPublished)
                .FirstOrDefaultAsync();

            if (hall is null)
            {
                return NotFound();
            }

            var imageEntities = await _db.HallImages
                .AsNoTracking()
                .Where(i => i.WeddingHallId == id)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .ToListAsync();

            var images = imageEntities
                .Select(i => new HallImageViewModel(
                    i.Id,
                    _imageUrl.Resolve(i),
                    i.AltText,
                    i.IsPrimary))
                .ToList();

            Hall = new HallViewModel(
                Id: hall.Id,
                Name: hall.Name,
                Slug: hall.Slug,
                ShortDescription: hall.ShortDescription,
                Description: hall.Description,
                CapacityMin: hall.CapacityMin,
                CapacityMax: hall.CapacityMax,
                Images: images,
                IsFromStaticCatalog: false);

            BackHref = StaticVenueHrefResolver.ResolveForVenue(hall.VenueKey);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "TiecCuoi/Details({Id}): database unavailable. {Message}",
                id,
                ex.Message);
            return NotFound();
        }
    }
}
