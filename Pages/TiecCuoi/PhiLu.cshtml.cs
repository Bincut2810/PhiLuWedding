using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Hall selection page for the Phì Lũ venue. Loads only the four
/// halls that belong to <see cref="VenueKeys.PhiLu"/> (Amber, Pearl,
/// Diamond, Crystal) in deterministic <c>SortOrder</c> order.
/// </summary>
public class PhiLuModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<PhiLuModel> _logger;

    public IReadOnlyList<GoldenPhoenixModel.VenueHallCardViewModel> Halls { get; private set; } =
        Array.Empty<GoldenPhoenixModel.VenueHallCardViewModel>();

    public bool DatabaseAvailable { get; private set; } = true;

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
        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogInformation(
                "PhiLu halls: no database context registered, showing empty state.");
            return;
        }

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

            Halls = rows.Select(r => new GoldenPhoenixModel.VenueHallCardViewModel(
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
                "PhiLu halls: database unavailable, showing empty state. {Message}",
                ex.Message);
        }
    }
}
