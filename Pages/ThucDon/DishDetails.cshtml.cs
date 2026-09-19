using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public detail page for a single <see cref="Dish"/>. The route is
/// overridden in <c>DishDetails.cshtml</c> via
/// <c>@page "/thuc-don/mon-an/{id:int}"</c>. The page lists every
/// published combo that includes the dish so the customer can see how
/// the dish fits into real menus.
/// </summary>
public class DishDetailsModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IDishImageUrlResolver _imageUrl;
    private readonly ILogger<DishDetailsModel> _logger;

    public DishViewModel? Dish { get; private set; }

    public bool DatabaseAvailable { get; private set; } = true;

    public DishDetailsModel(
        IDishImageUrlResolver imageUrl,
        ILogger<DishDetailsModel> logger,
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
            var dish = await _db.Dishes
                .AsNoTracking()
                .Where(d => d.Id == id && d.IsPublished)
                .FirstOrDefaultAsync();

            if (dish is null)
            {
                return NotFound();
            }

            // Gallery images (primary first, then sorted).
            var images = await _db.DishImages
                .AsNoTracking()
                .Where(i => i.DishId == id)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .ToListAsync();

            var imageVms = images
                .Select(i => new DishImageViewModel(
                    i.Id,
                    _imageUrl.Resolve(i),
                    i.AltText,
                    i.IsPrimary))
                .ToList();

            // Combos that include this dish, only through published menus.
            var combos = await _db.MenuComboDishes
                .AsNoTracking()
                .Where(cd => cd.DishId == id
                    && cd.MenuCombo != null
                    && cd.MenuCombo.IsPublished
                    && cd.MenuCombo.MenuSet != null
                    && cd.MenuCombo.MenuSet.IsPublished)
                .OrderBy(cd => cd.MenuCombo!.SortOrder)
                .ThenBy(cd => cd.MenuComboId)
                .Select(cd => new
                {
                    cd.MenuComboId,
                    cd.Quantity,
                    cd.Note,
                    Combo = cd.MenuCombo!,
                    ParentMenuSetId = cd.MenuCombo!.MenuSetId,
                    ParentMenuSetName = cd.MenuCombo!.MenuSet!.Name,
                    ParentMenuSetSlug = cd.MenuCombo!.MenuSet!.Slug
                })
                .ToListAsync();

            var comboVms = combos.Select(c => new DishComboViewModel(
                c.Combo.Id,
                c.Combo.Name,
                c.Combo.Slug,
                c.Combo.Price,
                c.Quantity,
                c.Note,
                new MenuSetSummary(ParentMenuSetId: c.ParentMenuSetId,
                                   ParentMenuSetName: c.ParentMenuSetName,
                                   ParentMenuSetSlug: c.ParentMenuSetSlug))).ToList();

            Dish = new DishViewModel(
                dish.Id,
                dish.Name,
                dish.Slug,
                dish.Description,
                dish.Ingredients,
                dish.Price,
                imageVms,
                comboVms);

            return Page();
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            _logger.LogWarning(
                "ThucDon/DishDetails({Id}): database unavailable. {Message}",
                id,
                ex.Message);
            return NotFound();
        }
    }

    /// <summary>Flat view model used by the Razor view.</summary>
    public sealed record DishViewModel(
        int Id,
        string Name,
        string Slug,
        string? Description,
        string? Ingredients,
        decimal? Price,
        IReadOnlyList<DishImageViewModel> Images,
        IReadOnlyList<DishComboViewModel> Combos);

    public sealed record DishImageViewModel(
        int Id,
        string? Url,
        string? AltText,
        bool IsPrimary);

    public sealed record DishComboViewModel(
        int Id,
        string Name,
        string Slug,
        decimal? Price,
        int Quantity,
        string? Note,
        MenuSetSummary ParentMenuSet);

    public sealed record MenuSetSummary(
        int ParentMenuSetId,
        string ParentMenuSetName,
        string ParentMenuSetSlug);
}