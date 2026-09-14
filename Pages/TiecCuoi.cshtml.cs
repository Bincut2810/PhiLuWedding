using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages;

/// <summary>
/// Public listing of all published wedding halls at Phì Lũ.
/// Reads from <see cref="WeddingHall"/> through <see cref="PhiluWeddingDbContext"/>;
/// no caching layer is added at this stage.
/// </summary>
public class TiecCuoiModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IHallImageUrlResolver _imageUrl;
    private readonly ILogger<TiecCuoiModel> _logger;

    /// <summary>The list of halls to render on the page.</summary>
    public IReadOnlyList<HallCardViewModel> Halls { get; private set; } =
        Array.Empty<HallCardViewModel>();

    /// <summary>True when the page is rendering its empty state.</summary>
    public bool ShowEmptyState => !DatabaseAvailable && Halls.Count == 0;

    /// <summary>True when the database connection failed; the page shows a polite notice instead of crashing.</summary>
    public bool DatabaseAvailable { get; private set; } = true;

    public TiecCuoiModel(
        IHallImageUrlResolver imageUrl,
        ILogger<TiecCuoiModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        // DbContext is optional: Program.cs only registers it when a
        // connection string is configured. The page degrades gracefully
        // when no context is available.
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
                "TiecCuoi listing: no database context registered, showing empty state.");
            return;
        }

        try
        {
            // AsNoTracking + single round-trip. We project into a flat
            // view model that already contains the primary image URL so
            // the Razor view never has to iterate collections.
            var rows = await _db.WeddingHalls
                .AsNoTracking()
                .Where(h => h.IsPublished)
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

            Halls = rows.Select(r =>
            {
                var image = _imageUrl.Resolve(r.PrimaryImage);
                return new HallCardViewModel(
                    r.Hall.Id,
                    r.Hall.Name,
                    r.Hall.Slug,
                    r.Hall.ShortDescription,
                    r.Hall.Description,
                    r.Hall.CapacityMin,
                    r.Hall.CapacityMax,
                    image,
                    r.PrimaryImage?.AltText);
            }).ToList();
        }
        catch (Exception ex)
        {
            // Any database-level failure (connection refused, missing
            // table, permissions, etc.) is treated as "database
            // unavailable". The page still renders with an empty state
            // and a polite notice, rather than 500.
            //
            // Log only the message — never the full exception object.
            // EF Core / Npgsql include host, port, and database name in
            // the exception payload, and the hosting platform captures
            // the full stack via its own sink.
            DatabaseAvailable = false;
            _logger.LogWarning(
                "TiecCuoi listing: database unavailable, showing empty state. {Message}",
                ex.Message);
        }
    }

    /// <summary>
    /// Flat projection of a hall plus its resolved primary image URL.
    /// Kept as a record so the Razor view is straightforward.
    /// </summary>
    public sealed record HallCardViewModel(
        int Id,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        int? CapacityMin,
        int? CapacityMax,
        string? ImageUrl,
        string? ImageAlt);
}