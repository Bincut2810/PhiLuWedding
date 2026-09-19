using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Hall selection page for the Golden Phoenix venue.
///
/// <para>Loading strategy (DB-first with static fallback):</para>
/// <list type="number">
///   <item>If the database is reachable AND has published halls tagged with
///         <see cref="VenueKeys.GoldenPhoenix"/>, those rows are rendered
///         (so future admin edits via the database flow straight through).</item>
///   <item>If the database is unreachable OR has no Golden Phoenix rows,
///         the four canonical halls from <see cref="StaticVenueCatalog"/>
///         are rendered instead. The static catalogue is the single source
///         of business truth in this scenario and is kept in sync with
///         <see cref="DbSeeder"/>.</item>
/// </list>
/// </summary>
public class GoldenPhoenixModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<GoldenPhoenixModel> _logger;

    public IReadOnlyList<VenueHallCardViewModel> Halls { get; private set; } =
        Array.Empty<VenueHallCardViewModel>();

    /// <summary>
    /// True when the page rendered from the live database. False when the
    /// static catalogue was used as a fallback. Useful for diagnostics.
    /// </summary>
    public bool RenderedFromDatabase { get; private set; }

    public GoldenPhoenixModel(
        IHallImageUrlResolver imageUrl,
        ILogger<GoldenPhoenixModel> logger,
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
                    .Where(h => h.IsPublished && h.VenueKey == VenueKeys.GoldenPhoenix)
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
                    Halls = rows.Select(r => new VenueHallCardViewModel(
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
                    "GoldenPhoenix halls: database query failed, falling back to static catalogue. {Message}",
                    ex.Message);
            }
        }

        // Static catalogue fallback (no DB rows or DB unavailable).
        Halls = StaticVenueCatalog
            .GetHalls(VenueKeys.GoldenPhoenix)
            .Select(r => new VenueHallCardViewModel(
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

    /// <summary>Flat projection used by the Razor view.</summary>
    public sealed record VenueHallCardViewModel(
        int Id,
        string Name,
        string Slug,
        int? CapacityMin,
        int? CapacityMax,
        string? ImageUrl,
        string? ImageAlt,
        string DetailHref,
        bool IsFromStaticCatalog);
}
