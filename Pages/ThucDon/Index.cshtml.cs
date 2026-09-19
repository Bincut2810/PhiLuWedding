using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public listing of all published <see cref="MenuSet"/>s at Phì Lũ.
/// Reads from the database through <see cref="PhiluWeddingDbContext"/>;
/// no caching layer is added at this stage. The page degrades
/// gracefully when the database is unavailable or has no rows yet.
/// </summary>
public class IndexModel : PageModel
{
    private readonly PhiluWeddingDbContext? _db;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>The list of menu sets to render on the page.</summary>
    public IReadOnlyList<MenuSetCardViewModel> MenuSets { get; private set; } =
        Array.Empty<MenuSetCardViewModel>();

    /// <summary>True when the database connection failed; the page shows a polite notice instead of crashing.</summary>
    public bool DatabaseAvailable { get; private set; } = true;

    public IndexModel(
        ILogger<IndexModel> logger,
        PhiluWeddingDbContext? db = null)
    {
        // DbContext is optional: Program.cs only registers it when a
        // connection string is configured. The page degrades gracefully
        // when no context is available.
        _db = db;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        if (_db is null)
        {
            DatabaseAvailable = false;
            _logger.LogInformation(
                "ThucDon listing: no database context registered, showing empty state.");
            return;
        }

        try
        {
            // AsNoTracking + single round-trip. Combo count is projected
            // so the Razor view never has to iterate collections.
            var rows = await _db.MenuSets
                .AsNoTracking()
                .Where(m => m.IsPublished)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .Select(m => new
                {
                    MenuSet = m,
                    ComboCount = m.Combos.Count(c => c.IsPublished)
                })
                .ToListAsync();

            MenuSets = rows.Select(r => new MenuSetCardViewModel(
                r.MenuSet.Id,
                r.MenuSet.Name,
                r.MenuSet.Slug,
                r.MenuSet.ShortDescription,
                r.MenuSet.Description,
                r.ComboCount)).ToList();
        }
        catch (Exception ex)
        {
            // Any database-level failure (connection refused, missing
            // table, permissions, etc.) is treated as "database
            // unavailable". The page still renders with an empty state
            // and a polite notice, rather than 500.
            //
            // Log only the message — never the full exception object.
            DatabaseAvailable = false;
            _logger.LogWarning(
                "ThucDon listing: database unavailable, showing empty state. {Message}",
                ex.Message);
        }
    }

    /// <summary>
    /// Flat projection of a menu set plus its published combo count.
    /// Kept as a record so the Razor view is straightforward.
    /// </summary>
    public sealed record MenuSetCardViewModel(
        int Id,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        int ComboCount);
}