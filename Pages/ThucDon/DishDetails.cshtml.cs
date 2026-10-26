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
            // The public scope keeps excluded wedding-menu options out of
            // the list, so the dish page never links to a hidden option.
            var scope = await PublicMenuScope.CreateFilterAsync(_db);

            var publishedMenuSetIds = await _db.MenuSets
                .AsNoTracking()
                .Where(m => m.IsPublished)
                .Select(m => m.Id)
                .ToListAsync();

            var eligibleComboIds = await scope.ApplyTo(_db.MenuCombos.AsNoTracking())
                .Where(c => c.IsPublished && publishedMenuSetIds.Contains(c.MenuSetId))
                .Select(c => c.Id)
                .ToListAsync();

            // The links, the option and its menu set are read with explicit
            // joins, so the query never depends on a navigation property
            // being loaded.
            var combos = await (from link in _db.MenuComboDishes.AsNoTracking()
                                where link.DishId == id && eligibleComboIds.Contains(link.MenuComboId)
                                join combo in _db.MenuCombos.AsNoTracking() on link.MenuComboId equals combo.Id
                                join menuSet in _db.MenuSets.AsNoTracking() on combo.MenuSetId equals menuSet.Id
                                orderby combo.SortOrder, link.MenuComboId
                                select new
                                {
                                    link.MenuComboId,
                                    link.Quantity,
                                    link.Note,
                                    ComboId = combo.Id,
                                    ComboName = combo.Name,
                                    ComboSlug = combo.Slug,
                                    ComboPrice = combo.Price,
                                    ParentMenuSetId = menuSet.Id,
                                    ParentMenuSetName = menuSet.Name,
                                    ParentMenuSetSlug = menuSet.Slug
                                })
                .ToListAsync();

            var comboVms = combos.Select(c => new DishComboViewModel(
                c.ComboId,
                c.ComboName,
                c.ComboSlug,
                c.ComboPrice,
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