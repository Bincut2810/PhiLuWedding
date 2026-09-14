using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Public detail page for a single <see cref="WeddingHall"/>. The
/// route is overridden in <c>Details.cshtml</c> via
/// <c>@page "/tiec-cuoi/{id:int}"</c> so the URL is
/// <c>/tiec-cuoi/&lt;id&gt;</c> while the file still lives at
/// <c>Pages/TiecCuoi/Details.cshtml</c>.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<DetailsModel> _logger;

    public HallViewModel? Hall { get; private set; }

    public bool DatabaseAvailable { get; private set; } = true;

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
            DatabaseAvailable = false;
            return NotFound();
        }

        try
        {
            // AsNoTracking, single round-trip. Only published halls are
            // publicly visible.
            var hall = await _db.WeddingHalls
                .AsNoTracking()
                .Where(h => h.Id == id && h.IsPublished)
                .FirstOrDefaultAsync();

            if (hall is null)
            {
                return NotFound();
            }

            // Images are loaded in a separate, sorted query — also
            // AsNoTracking — so the gallery order is deterministic
            // and we avoid pulling the entire entity graph. The URL
            // resolver runs in-memory after the SQL projection so EF
            // does not have to translate a managed method.
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
                hall.Id,
                hall.Name,
                hall.Slug,
                hall.ShortDescription,
                hall.Description,
                hall.CapacityMin,
                hall.CapacityMax,
                images);

            return Page();
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            // Log only the message — never the full exception object —
            // to avoid surfacing EF Core / Npgsql connection metadata
            // (host, port, database name) in production logs.
            _logger.LogWarning(
                "TiecCuoi/Details({Id}): database unavailable. {Message}",
                id,
                ex.Message);
            return NotFound();
        }
    }

    /// <summary>Flat view model used by the Razor view.</summary>
    public sealed record HallViewModel(
        int Id,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        int? CapacityMin,
        int? CapacityMax,
        IReadOnlyList<HallImageViewModel> Images);

    public sealed record HallImageViewModel(
        int Id,
        string? Url,
        string? AltText,
        bool IsPrimary);
}