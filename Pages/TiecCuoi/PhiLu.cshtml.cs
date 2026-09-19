using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Hall selection page for the Trung tâm Hội nghị Tiệc cưới Phì Lũ venue.
///
/// <para>Loading strategy (DB-first with static fallback) — mirrors
/// <see cref="GoldenPhoenixModel"/>. When the database is unavailable or
/// has no Phì Lũ rows, the four canonical halls (Amber, Pearl, Diamond,
/// Crystal) from <see cref="StaticVenueCatalog"/> are rendered.</para>
/// </summary>
public class PhiLuModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<PhiLuModel> _logger;

    public IReadOnlyList<GoldenPhoenixModel.VenueHallCardViewModel> Halls { get; private set; } =
        Array.Empty<GoldenPhoenixModel.VenueHallCardViewModel>();

    /// <summary>True when the page rendered from the live database.</summary>
    public bool RenderedFromDatabase { get; private set; }

    public PhiLuModel(
        IHallImageUrlResolver imageUrl,
        ILogger<PhiLuModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        _db = db;
        _imageUrl = imageUrl;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if (_db is not null)
        {
            try
            {
                var rows = await _db.WeddingHalls
                    .AsNoTracking()
                    .Where(h => h.IsPublished && h.VenueKey == VenueKeys.PhiLu)
                    .OrderBy(h => h.SortOrder)
                    .ThenBy(h => h.Id)
                    .Select(h => new
                    {
                        Hall = h,
                        PrimaryImage = h.Images
                            .OrderByDescending(i => i.IsPrimary)
                            .ThenBy(i => i.SortOrder)
                            .ThenBy(i => i.Id)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                if (rows.Count > 0)
                {
                    Halls = rows.Select(r => new GoldenPhoenixModel.VenueHallCardViewModel(
                        Id: r.Hall.Id,
                        Name: r.Hall.Name,
                        Slug: r.Hall.Slug,
                        CapacityMin: r.Hall.CapacityMin,
                        CapacityMax: r.Hall.CapacityMax,
                        ImageUrl: _imageUrl.Resolve(r.PrimaryImage),
                        ImageAlt: r.PrimaryImage?.AltText,
                        DetailHref: $"/tiec-cuoi/{r.Hall.Id}",
                        IsFromStaticCatalog: false)).ToList();
                    RenderedFromDatabase = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "PhiLu halls: database query failed, falling back to static catalogue. {Message}",
                    ex.Message);
            }
        }

        // Static catalogue fallback.
        Halls = StaticVenueCatalog
            .GetHalls(VenueKeys.PhiLu)
            .Select(r => new GoldenPhoenixModel.VenueHallCardViewModel(
                Id: r.Id,
                Name: r.Name,
                Slug: r.Slug,
                CapacityMin: r.CapacityMin,
                CapacityMax: r.CapacityMax,
                ImageUrl: r.ImageUrl,
                ImageAlt: r.ImageAlt,
                DetailHref: $"/tiec-cuoi/hall/{r.Slug}",
                IsFromStaticCatalog: true))
            .ToList();
        RenderedFromDatabase = false;
    }
}
