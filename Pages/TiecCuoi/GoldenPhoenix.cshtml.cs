using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Hall selection page for the Golden Phoenix venue. Loads only the
/// four halls that belong to <see cref="VenueKeys.GoldenPhoenix"/>, in
/// deterministic <c>SortOrder</c> order. Each card links to the
/// existing detail route <c>/tiec-cuoi/{id}</c>.
/// </summary>
public class GoldenPhoenixModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<GoldenPhoenixModel> _logger;

    public IReadOnlyList<VenueHallCardViewModel> Halls { get; private set; } =
        Array.Empty<VenueHallCardViewModel>();

    public bool DatabaseAvailable { get; private set; } = true;

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
        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogInformation(
                "GoldenPhoenix halls: no database context registered, showing empty state.");
            return;
        }

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

            Halls = rows.Select(r => new VenueHallCardViewModel(
                r.Hall.Id,
                r.Hall.Name,
                r.Hall.CapacityMin,
                r.Hall.CapacityMax,
                _imageUrl.Resolve(r.PrimaryImage),
                r.PrimaryImage?.AltText)).ToList();
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            _logger.LogWarning(
                "GoldenPhoenix halls: database unavailable, showing empty state. {Message}",
                ex.Message);
        }
    }

    /// <summary>Flat projection used by the Razor view.</summary>
    public sealed record VenueHallCardViewModel(
        int Id,
        string Name,
        int? CapacityMin,
        int? CapacityMax,
        string? ImageUrl,
        string? ImageAlt);
}
