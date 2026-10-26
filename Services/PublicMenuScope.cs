using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Services;

/// <summary>
/// The public scope of the wedding-menu catalog.
///
/// <para>
/// The business sells exactly four wedding-menu price levels with nine menu
/// options in total. This class is the single place that states which
/// price tiers and which option codes are public, so every public route
/// (<c>/thuc-don</c>, <c>/thuc-don/{id}</c>, <c>/thuc-don/combo/{id}</c>,
/// <c>/thuc-don/mon-an/{id}</c>, <c>/thuc-don/philu</c> and
/// <c>/thuc-don/golden-phoenix</c>) applies exactly the same rule instead
/// of repeating a slightly different one.
/// </para>
///
/// <para>
/// A public menu option is eligible only when
/// <list type="number">
///   <item><description>its price tier is published,</description></item>
///   <item><description>the tier key is one of the four public tiers,</description></item>
///   <item><description>the option code is allowed for that tier,</description></item>
///   <item><description>the option itself is published, and</description></item>
///   <item><description>its parent menu set is published.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Rows that belong to no price tier at all (legacy demo rows of a menu
/// set that has no tiers) keep their existing behaviour and stay visible in
/// their own menu set. They are never interpreted as an option of a public
/// tier, and they never appear in the wedding menu set, because a tier-less
/// row of the wedding menu set is ambiguous and therefore not public.
/// </para>
/// </summary>
public static class PublicMenuScope
{
    /// <summary>
    /// The slug of the wedding menu set — the menu set the public scope
    /// applies to.
    /// </summary>
    public const string WeddingMenuSetSlug = "menu-tiec-cuoi";

    /// <summary>One public price tier and the option codes it sells.</summary>
    /// <param name="Key">The stable tier key, for example <c>tier-08</c>.</param>
    /// <param name="OptionCodes">The public option codes of the tier.</param>
    public sealed record TierScope(string Key, IReadOnlyList<string> OptionCodes);

    /// <summary>
    /// The four public price tiers and their option codes. The 5.000.000 VND
    /// tier keeps its third option (C); the other three tiers sell A and B.
    /// </summary>
    public static IReadOnlyList<TierScope> Tiers { get; } = new[]
    {
        new TierScope("tier-02", new[] { "A", "B" }),
        new TierScope("tier-04", new[] { "A", "B" }),
        new TierScope("tier-07", new[] { "A", "B" }),
        new TierScope("tier-08", new[] { "A", "B", "C" })
    };

    /// <summary>The stable keys of the four public price tiers.</summary>
    public static IReadOnlyList<string> TierKeys { get; } =
        Tiers.Select(t => t.Key).ToList();

    /// <summary>True when the tier key is one of the four public tiers.</summary>
    public static bool IsPublicTier(string? tierKey) =>
        tierKey is not null
        && Tiers.Any(t => string.Equals(t.Key, tierKey, StringComparison.Ordinal));

    /// <summary>
    /// The public option codes of a tier. An empty list is returned for every
    /// tier that is not public.
    /// </summary>
    public static IReadOnlyList<string> OptionCodesFor(string? tierKey)
    {
        if (tierKey is null)
        {
            return Array.Empty<string>();
        }

        var tier = Tiers.FirstOrDefault(t => string.Equals(t.Key, tierKey, StringComparison.Ordinal));

        return tier?.OptionCodes ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>
    /// True when the option code is one of the public options of the tier.
    /// </summary>
    public static bool IsPublicOption(string? tierKey, string? optionCode)
    {
        if (optionCode is null)
        {
            return false;
        }

        var codes = OptionCodesFor(tierKey);

        return codes.Contains(optionCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads the public scope from the database: the published public tiers
    /// (optionally limited to one venue) and the id of the wedding menu set.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="venueKey">
    /// When given, only tiers that apply to that venue are loaded (see
    /// <see cref="MenuVenueScopes"/>); <c>null</c> means "every venue".
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<PublicMenuFilter> CreateFilterAsync(
        PhiluWeddingDbContext db,
        string? venueKey = null,
        CancellationToken ct = default)
    {
        var tiers = await db.MenuTiers
            .AsNoTracking()
            .Where(t => t.IsPublished && TierKeys.Contains(t.Key))
            .Where(t => venueKey == null
                        || t.VenueScope == MenuVenueScopes.BothVenues
                        || t.VenueScope == venueKey)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        var weddingMenuSetId = await db.MenuSets
            .AsNoTracking()
            .Where(s => s.Slug == WeddingMenuSetSlug)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        return new PublicMenuFilter(weddingMenuSetId, tiers);
    }
}

/// <summary>
/// The public scope of the wedding menu for one request: the public price
/// tiers and the predicate that decides whether a menu option is public.
///
/// <para>
/// The predicate is built from data, not written out by hand, so the rule
/// cannot drift away from <see cref="PublicMenuScope"/>.
/// </para>
/// </summary>
public sealed class PublicMenuFilter
{
    private readonly Expression<Func<MenuCombo, bool>> _comboPredicate;

    internal PublicMenuFilter(int? weddingMenuSetId, IReadOnlyList<MenuTier> tiers)
    {
        WeddingMenuSetId = weddingMenuSetId;
        Tiers = tiers;
        _comboPredicate = BuildComboPredicate(weddingMenuSetId, tiers);
    }

    /// <summary>The id of the wedding menu set, when it exists.</summary>
    public int? WeddingMenuSetId { get; }

    /// <summary>The published public price tiers, in display order.</summary>
    public IReadOnlyList<MenuTier> Tiers { get; }

    /// <summary>The predicate that decides whether a menu option is public.</summary>
    public Expression<Func<MenuCombo, bool>> ComboPredicate => _comboPredicate;

    /// <summary>Applies the public scope to a query of menu options.</summary>
    public IQueryable<MenuCombo> ApplyTo(IQueryable<MenuCombo> query) =>
        query.Where(_comboPredicate);

    /// <summary>True when the tier is one of the public tiers of this scope.</summary>
    public bool IsPublicTier(MenuTier tier) =>
        Tiers.Any(t => t.Id == tier.Id);

    /// <summary>
    /// True when the option code is a public option of the given tier.
    /// </summary>
    public bool IsPublicOption(MenuTier tier, string? optionCode) =>
        PublicMenuScope.IsPublicOption(tier.Key, optionCode);

    /// <summary>
    /// Builds the predicate.
    ///
    /// <para>
    /// A menu option is public when its tier is one of the public tiers and
    /// its code is allowed for that tier. A menu option that belongs to no
    /// tier stays visible in a menu set that has no tiers of its own (the
    /// legacy demo rows), but never in the wedding menu set, because a
    /// tier-less row of the wedding menu set cannot be verified.
    /// </para>
    /// </summary>
    private static Expression<Func<MenuCombo, bool>> BuildComboPredicate(
        int? weddingMenuSetId,
        IReadOnlyList<MenuTier> tiers)
    {
        var parameter = Expression.Parameter(typeof(MenuCombo), "c");
        Expression? tiered = null;

        foreach (var tier in tiers)
        {
            var codes = PublicMenuScope.OptionCodesFor(tier.Key);

            if (codes.Count == 0)
            {
                continue;
            }

            Expression codeTest = Expression.Constant(false);

            foreach (var code in codes)
            {
                codeTest = Expression.OrElse(
                    codeTest,
                    Expression.Equal(
                        Expression.Property(parameter, nameof(MenuCombo.OptionCode)),
                        Expression.Constant(code)));
            }

            var group = Expression.AndAlso(
                Expression.Equal(
                    Expression.Property(parameter, nameof(MenuCombo.MenuTierId)),
                    Expression.Constant(tier.Id, typeof(int?))),
                codeTest);

            tiered = tiered is null ? group : Expression.OrElse(tiered, group);
        }

        // A menu option without a price tier is only kept when it does not
        // belong to the wedding menu set. It is never attached to a tier.
        Expression legacy = Expression.Constant(false);

        if (weddingMenuSetId is int menuSetId)
        {
            legacy = Expression.AndAlso(
                Expression.Equal(
                    Expression.Property(parameter, nameof(MenuCombo.MenuTierId)),
                    Expression.Constant(null, typeof(int?))),
                Expression.NotEqual(
                    Expression.Property(parameter, nameof(MenuCombo.MenuSetId)),
                    Expression.Constant(menuSetId)));
        }

        var body = tiered is null ? legacy : Expression.OrElse(tiered, legacy);

        return Expression.Lambda<Func<MenuCombo, bool>>(body, parameter);
    }
}
