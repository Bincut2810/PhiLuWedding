using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Services;

/// <summary>
/// Removes the content that is no longer part of the public wedding menu
/// from the database — without deleting anything.
///
/// <para>
/// The trimmed seed file <c>Seed/menu-catalog.json</c> is the source of
/// truth for the public menu: four price tiers and nine options. This
/// cleanup brings an existing database into the same state.
/// </para>
///
/// <para>
/// <b>Safety contract</b>
/// <list type="bullet">
///   <item><description>
///     Nothing is ever deleted: no dish, no dish image, no
///     <c>menu_combo_dishes</c> row, no menu option, no price tier and no
///     table.
///   </description></item>
///   <item><description>
///     Only the publication flag of the excluded rows is cleared, so the
///     cleanup is reversible: publishing a row again restores the old state.
///   </description></item>
///   <item><description>
///     The publication flag of a retained tier or option is never touched.
///   </description></item>
///   <item><description>
///     Rows are matched on their stable key (<c>tier-02</c>, …) and on the
///     option code inside their tier — never on a display name and never on
///     the row order.
///   </description></item>
///   <item><description>
///     A row that is expected but missing is reported, never created.
///   </description></item>
/// </list>
/// </para>
///
/// <para>
/// The command is opt-in. It only runs when it is invoked explicitly with
/// <c>--prune-menu-catalog</c>; it never runs during a build, during the
/// tests, on a normal application start or during a Railway deployment, and
/// it is not coupled to <c>--seed-menu</c>.
/// </para>
/// </summary>
public static class MenuCatalogPruner
{
    /// <summary>
    /// The price tiers that are no longer part of the public menu. Their
    /// options (A and B) are unpublished as well.
    /// </summary>
    public static IReadOnlyList<string> ExcludedTierKeys { get; } = new[]
    {
        "tier-01",
        "tier-03",
        "tier-05",
        "tier-06"
    };

    /// <summary>
    /// The option codes of the excluded price tiers. The 5.000.000 VND tier
    /// is not in the list, so its options A, B and C stay published.
    /// </summary>
    public static IReadOnlyList<string> ExcludedOptionCodes { get; } = new[]
    {
        "A",
        "B"
    };

    /// <summary>
    /// Cleans the menu catalog up and returns what it changed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">The logger the cleanup writes to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<MenuPruneReport> PruneAsync(
        PhiluWeddingDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        var report = new MenuPruneReport();
        var now = DateTime.UtcNow;
        var catalog = MenuCatalogSeeder.LoadCatalog();

        // The cleanup only writes publication flags and the sort order of the
        // retained tiers. A transaction makes sure a failure never leaves the
        // catalog half-way through the change.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(ct)
            : null;

        try
        {
            await UnpublishExcludedTiersAsync(db, report, now, ct);
            await UnpublishExcludedOptionsAsync(db, report, now, ct);
            await NormalizeRetainedTiersAsync(db, catalog, report, now, ct);
            await VerifyRetainedOptionsAsync(db, catalog, report, ct);
            await UnpublishSupportingSectionsAsync(db, report, now, ct);
            await UpdateMenuSetDescriptionAsync(db, catalog, report, now, ct);
            await ReportLegacyOptionsAsync(db, report, ct);

            await db.SaveChangesAsync(ct);

            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(ct);
            }

            throw;
        }

        logger.LogInformation("MenuCatalogPruner: finished. {Summary}", report.Summary());

        foreach (var conflict in report.Conflicts)
        {
            logger.LogWarning("MenuCatalogPruner conflict: {Conflict}", conflict);
        }

        foreach (var error in report.Errors)
        {
            logger.LogWarning("MenuCatalogPruner error: {Error}", error);
        }

        return report;
    }

    // ---------------------------------------------------------------------
    // Excluded price tiers
    // ---------------------------------------------------------------------

    private static async Task UnpublishExcludedTiersAsync(
        PhiluWeddingDbContext db,
        MenuPruneReport report,
        DateTime now,
        CancellationToken ct)
    {
        foreach (var key in ExcludedTierKeys)
        {
            var tier = await db.MenuTiers.FirstOrDefaultAsync(t => t.Key == key, ct);

            if (tier is null)
            {
                report.CountMissing("excludedTiers");
                report.Errors.Add(
                    $"Price tier '{key}' does not exist. It was not created, because the cleanup never creates rows.");
                continue;
            }

            if (!tier.IsPublished)
            {
                report.CountUnchanged("excludedTiers");
                continue;
            }

            tier.IsPublished = false;
            tier.UpdatedAt = now;
            report.CountUpdated("excludedTiers");
        }
    }

    // ---------------------------------------------------------------------
    // Excluded menu options
    // ---------------------------------------------------------------------

    private static async Task UnpublishExcludedOptionsAsync(
        PhiluWeddingDbContext db,
        MenuPruneReport report,
        DateTime now,
        CancellationToken ct)
    {
        var excludedTierIds = await db.MenuTiers
            .Where(t => ExcludedTierKeys.Contains(t.Key))
            .Select(t => t.Id)
            .ToListAsync(ct);

        if (excludedTierIds.Count == 0)
        {
            report.CountMissing("excludedOptions");
            return;
        }

        var options = await db.MenuCombos
            .Where(c => c.MenuTierId != null && excludedTierIds.Contains(c.MenuTierId!.Value))
            .ToListAsync(ct);

        foreach (var option in options)
        {
            var code = option.OptionCode?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(code))
            {
                report.Conflicts.Add(
                    $"Menu option {option.Id} belongs to an excluded price tier but has no option code. It was left untouched.");
                continue;
            }

            if (!ExcludedOptionCodes.Contains(code))
            {
                report.Conflicts.Add(
                    $"Menu option '{code}' of an excluded price tier is not part of the excluded set; it was left published.");
                continue;
            }

            if (!option.IsPublished)
            {
                report.CountUnchanged("excludedOptions");
                continue;
            }

            option.IsPublished = false;
            option.UpdatedAt = now;
            report.CountUpdated("excludedOptions");
        }

        // Every excluded tier sells the options A and B. A missing option is
        // reported, never created.
        foreach (var code in ExcludedOptionCodes)
        {
            var present = await db.MenuCombos
                .Where(c => c.MenuTierId != null
                    && excludedTierIds.Contains(c.MenuTierId!.Value)
                    && c.OptionCode == code)
                .AnyAsync(ct);

            if (!present)
            {
                report.CountMissing("excludedOptions");
            }
        }
    }

    // ---------------------------------------------------------------------
    // Retained price tiers
    // ---------------------------------------------------------------------

    private static async Task NormalizeRetainedTiersAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        MenuPruneReport report,
        DateTime now,
        CancellationToken ct)
    {
        foreach (var definition in catalog.Tiers)
        {
            var tier = await db.MenuTiers.FirstOrDefaultAsync(t => t.Key == definition.Key, ct);

            if (tier is null)
            {
                report.CountMissing("retainedTiers");
                report.Errors.Add(
                    $"Price tier '{definition.Key}' does not exist. It was not created, because the cleanup never creates rows.");
                continue;
            }

            // The publication flag of a retained tier is never touched.
            if (tier.SortOrder != definition.SortOrder)
            {
                tier.SortOrder = definition.SortOrder;
                tier.UpdatedAt = now;
                report.CountUpdated("retainedTiers");
            }
            else
            {
                report.CountUnchanged("retainedTiers");
            }
        }
    }

    // ---------------------------------------------------------------------
    // Retained menu options
    // ---------------------------------------------------------------------

    private static async Task VerifyRetainedOptionsAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        MenuPruneReport report,
        CancellationToken ct)
    {
        foreach (var tierDefinition in catalog.Tiers)
        {
            var tier = await db.MenuTiers.FirstOrDefaultAsync(t => t.Key == tierDefinition.Key, ct);

            if (tier is null)
            {
                continue;
            }

            foreach (var optionDefinition in tierDefinition.Options)
            {
                var code = optionDefinition.Code?.Trim().ToUpperInvariant() ?? string.Empty;

                var option = await db.MenuCombos
                    .FirstOrDefaultAsync(c => c.MenuTierId == tier.Id && c.OptionCode == code, ct);

                if (option is null)
                {
                    report.CountMissing("retainedOptions");
                    report.Errors.Add(
                        $"Menu option '{code}' of tier '{tierDefinition.Key}' does not exist. It was not created, because the cleanup never creates rows.");
                    continue;
                }

                // The publication flag of a retained option is never touched.
                report.CountUnchanged("retainedOptions");
            }
        }
    }

    // ---------------------------------------------------------------------
    // Supporting sections
    // ---------------------------------------------------------------------

    private static async Task UnpublishSupportingSectionsAsync(
        PhiluWeddingDbContext db,
        MenuPruneReport report,
        DateTime now,
        CancellationToken ct)
    {
        // The rows of the supporting sections are unpublished; they are
        // never deleted. The rows stay in the database, so publishing them
        // again restores the previous state.
        await UnpublishAsync(
            db.MenuServices,
            s => s.IsPublished,
            s =>
            {
                s.IsPublished = false;
                s.UpdatedAt = now;
            },
            "services",
            report,
            ct);

        await UnpublishAsync(
            db.MenuBeverages,
            b => b.IsPublished,
            b =>
            {
                b.IsPublished = false;
                b.UpdatedAt = now;
            },
            "beverages",
            report,
            ct);

        await UnpublishAsync(
            db.MenuAppetizerNuts,
            n => n.IsPublished,
            n =>
            {
                n.IsPublished = false;
                n.UpdatedAt = now;
            },
            "nuts",
            report,
            ct);

        // A promotion benefit has no publication flag of its own — it is
        // published through its promotion tier, which is unpublished here.
        await UnpublishAsync(
            db.MenuPromotions,
            p => p.IsPublished,
            p =>
            {
                p.IsPublished = false;
                p.UpdatedAt = now;
            },
            "promotions",
            report,
            ct);

        await UnpublishAsync(
            db.MenuPromotionTerms,
            t => t.IsPublished,
            t =>
            {
                t.IsPublished = false;
                t.UpdatedAt = now;
            },
            "promotionTerms",
            report,
            ct);

        await UnpublishAsync(
            db.MenuDepositTerms,
            t => t.IsPublished,
            t =>
            {
                t.IsPublished = false;
                t.UpdatedAt = now;
            },
            "depositTerms",
            report,
            ct);
    }

    // ---------------------------------------------------------------------
    // Menu set description
    // ---------------------------------------------------------------------

    private static async Task UpdateMenuSetDescriptionAsync(
        PhiluWeddingDbContext db,
        MenuCatalogDocument catalog,
        MenuPruneReport report,
        DateTime now,
        CancellationToken ct)
    {
        var definition = catalog.MenuSet;

        if (definition is null || string.IsNullOrWhiteSpace(definition.Slug))
        {
            report.Errors.Add("The seed file has no 'menuSet' section.");
            return;
        }

        var menuSet = await db.MenuSets.FirstOrDefaultAsync(s => s.Slug == definition.Slug, ct);

        if (menuSet is null)
        {
            report.CountMissing("menuSet");
            report.Errors.Add(
                $"Menu set '{definition.Slug}' does not exist. It was not created, because the cleanup never creates rows.");
            return;
        }

        if (string.Equals(menuSet.Description, definition.Description, StringComparison.Ordinal))
        {
            report.CountUnchanged("menuSet");
            return;
        }

        menuSet.Description = definition.Description;
        menuSet.UpdatedAt = now;
        report.CountUpdated("menuSet");
    }

    // ---------------------------------------------------------------------
    // Legacy rows
    // ---------------------------------------------------------------------

    private static async Task ReportLegacyOptionsAsync(
        PhiluWeddingDbContext db,
        MenuPruneReport report,
        CancellationToken ct)
    {
        // A menu option that belongs to no price tier is ambiguous: it is
        // never attached to a tier and never deleted. Rows without an option
        // code are clearly not tier options, so only the rows that could be
        // mistaken for one are reported.
        var legacy = await db.MenuCombos
            .Where(c => c.MenuTierId == null && c.OptionCode != null)
            .Select(c => new { c.Id, c.OptionCode, c.MenuSetId })
            .ToListAsync(ct);

        foreach (var option in legacy)
        {
            report.Conflicts.Add(
                $"Menu option {option.Id} belongs to no price tier (menu set {option.MenuSetId}, code '{option.OptionCode ?? "none"}'). It was not attached to a tier and not deleted.");
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static async Task UnpublishAsync<T>(
        DbSet<T> rows,
        Func<T, bool> isPublished,
        Action<T> unpublish,
        string subject,
        MenuPruneReport report,
        CancellationToken ct)
        where T : class
    {
        var items = await rows.ToListAsync(ct);

        foreach (var item in items)
        {
            if (!isPublished(item))
            {
                report.CountUnchanged(subject);
                continue;
            }

            unpublish(item);
            report.CountUpdated(subject);
        }
    }
}
