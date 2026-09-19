using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public detail page for a single <see cref="MenuCombo"/>. The route is
/// overridden in <c>ComboDetails.cshtml</c> via
/// <c>@page "/thuc-don/combo/{id:int}"</c>. The page lists every dish in
/// the combo and links each dish to its own detail page.
/// </summary>
public class ComboDetailsModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly IDishImageUrlResolver _imageUrl;
    private readonly ILogger<ComboDetailsModel> _logger;

    public ComboViewModel? Combo { get; private set; }

    public bool DatabaseAvailable { get; private set; } = true;

    public ComboDetailsModel(
        IDishImageUrlResolver imageUrl,
        ILogger<ComboDetailsModel> logger,
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
            var combo = await _db.MenuCombos
                .AsNoTracking()
                .Where(c => c.Id == id && c.IsPublished)
                .FirstOrDefaultAsync();

            if (combo is null)
            {
                return NotFound();
            }

            // Parent menu set (used for breadcrumbs) — silent when missing.
            var menuSet = await _db.MenuSets
                .AsNoTracking()
                .Where(m => m.Id == combo.MenuSetId && m.IsPublished)
                .Select(m => new { m.Id, m.Name, m.Slug })
                .FirstOrDefaultAsync();

            // Dishes in the combo with their primary image.
            var dishes = await _db.MenuComboDishes
                .AsNoTracking()
                .Where(cd => cd.MenuComboId == id && cd.Dish != null && cd.Dish.IsPublished)
                .OrderBy(cd => cd.SortOrder)
                .ThenBy(cd => cd.DishId)
                .Select(cd => new
                {
                    Link = cd,
                    Dish = cd.Dish!,
                    PrimaryImage = cd.Dish!.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .ThenBy(i => i.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var dishVms = dishes.Select(d =>
            {
                var image = _imageUrl.Resolve(d.PrimaryImage);
                return new ComboDishViewModel(
                    d.Dish.Id,
                    d.Dish.Name,
                    d.Dish.Slug,
                    d.Dish.Description,
                    d.Dish.Ingredients,
                    d.Dish.Price,
                    image,
                    d.PrimaryImage?.AltText,
                    d.Link.Quantity,
                    d.Link.Note);
            }).ToList();

            Combo = new ComboViewModel(
                combo.Id,
                combo.Name,
                combo.Slug,
                combo.Description,
                combo.Price,
                combo.GuestCount,
                menuSet is null ? null : new MenuSetSummary(menuSet.Id, menuSet.Name, menuSet.Slug),
                dishVms);

            return Page();
        }
        catch (Exception ex)
        {
            DatabaseAvailable = false;
            _logger.LogWarning(
                "ThucDon/ComboDetails({Id}): database unavailable. {Message}",
                id,
                ex.Message);
            return NotFound();
        }
    }

    /// <summary>Flat view model used by the Razor view.</summary>
    public sealed record ComboViewModel(
        int Id,
        string Name,
        string Slug,
        string? Description,
        decimal? Price,
        int? GuestCount,
        MenuSetSummary? ParentMenuSet,
        IReadOnlyList<ComboDishViewModel> Dishes);

    public sealed record MenuSetSummary(int Id, string Name, string Slug);

    public sealed record ComboDishViewModel(
        int Id,
        string Name,
        string Slug,
        string? Description,
        string? Ingredients,
        decimal? Price,
        string? ImageUrl,
        string? ImageAlt,
        int Quantity,
        string? Note);
}