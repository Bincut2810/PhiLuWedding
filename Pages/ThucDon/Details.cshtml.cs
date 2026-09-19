using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public detail page for a single <see cref="MenuSet"/>. The route is
/// overridden in <c>Details.cshtml</c> via <c>@page "/thuc-don/{id:int}"</c>
/// so the URL is <c>/thuc-don/&lt;id&gt;</c> while the file still lives
/// at <c>Pages/ThucDon/Details.cshtml</c>. The page shows all published
/// combos inside the set; each combo links to its own detail page.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IDishImageUrlResolver _imageUrl;
    private readonly ILogger<DetailsModel> _logger;

    public MenuSetViewModel? MenuSet { get; private set; }

    public bool DatabaseAvailable { get; private set; } = true;

    public DetailsModel(
        IDishImageUrlResolver imageUrl,
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
            var menuSet = await _db.MenuSets
                .AsNoTracking()
                .Where(m => m.Id == id && m.IsPublished)
                .FirstOrDefaultAsync();

            if (menuSet is null)
            {
                return NotFound();
            }

            // Load combos with their dishes in a separate query so the
            // gallery order is deterministic. The image URL resolver
            // runs in-memory after the SQL projection so EF does not
            // have to translate a managed method.
            var combos = await _db.MenuCombos
                .AsNoTracking()
                .Where(c => c.MenuSetId == id && c.IsPublished)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .Select(c => new
                {
                    Combo = c,
                    Dishes = c.ComboDishes
                        .Where(cd => cd.Dish != null && cd.Dish.IsPublished)
                        .OrderBy(cd => cd.SortOrder)
                        .ThenBy(cd => cd.DishId)
                        .Select(cd => new
                        {
                            cd.DishId,
                            cd.SortOrder,
                            cd.Quantity,
                            cd.Note,
                            Dish = cd.Dish!,
                            PrimaryImage = cd.Dish!.Images
                                .OrderByDescending(i => i.IsPrimary)
                                .ThenBy(i => i.SortOrder)
                                .ThenBy(i => i.Id)
                                .FirstOrDefault()
                        })
                        .ToList()
                })
                .ToListAsync();

            var comboVms = combos.Select(c => new ComboCardViewModel(
                c.Combo.Id,
                c.Combo.Name,
                c.Combo.Slug,
                c.Combo.Description,
                c.Combo.Price,
                c.Combo.GuestCount,
                c.Dishes.Select(d =>
                {
                    var image = _imageUrl.Resolve(d.PrimaryImage);
                    return new ComboDishPreviewViewModel(
                        d.DishId,
                        d.Dish.Name,
                        d.Dish.Slug,
                        image,
                        d.PrimaryImage?.AltText,
                        d.Quantity,
                        d.Note);
                }).ToList()
            )).ToList();

            MenuSet = new MenuSetViewModel(
                menuSet.Id,
                menuSet.Name,
                menuSet.Slug,
                menuSet.ShortDescription,
                menuSet.Description,
                comboVms);

            return Page();
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            // Log only the message — never the full exception object —
            // to avoid surfacing EF Core / Npgsql connection metadata
            // (host, port, database name) in production logs.
            _logger.LogWarning(
                "ThucDon/Details({Id}): database unavailable. {Message}",
                id,
                ex.Message);
            return NotFound();
        }
    }

    /// <summary>Flat view model used by the Razor view.</summary>
    public sealed record MenuSetViewModel(
        int Id,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        IReadOnlyList<ComboCardViewModel> Combos);

    public sealed record ComboCardViewModel(
        int Id,
        string Name,
        string Slug,
        string? Description,
        decimal? Price,
        int? GuestCount,
        IReadOnlyList<ComboDishPreviewViewModel> DishPreviews);

    public sealed record ComboDishPreviewViewModel(
        int Id,
        string Name,
        string Slug,
        string? ImageUrl,
        string? ImageAlt,
        int Quantity,
        string? Note);
}