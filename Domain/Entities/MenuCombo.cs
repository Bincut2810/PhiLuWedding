using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A priced menu combo inside a <see cref="MenuSet"/>.
///
/// <para>
/// For the wedding menu a combo is one <em>option</em> (A, B or C) of a
/// price <see cref="MenuTier"/>. The option carries the fixed dish
/// composition; the price itself lives on the tier so that a price change
/// never touches dish data.
/// </para>
/// </summary>
public class MenuCombo
{
    public int Id { get; set; }

    public int MenuSetId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Price in VND. Stored as numeric(18,2) in PostgreSQL.
    ///
    /// <para>
    /// For tiered options (a combo that points at a <see cref="MenuTier"/>)
    /// this column is intentionally left null: the authoritative price of the
    /// option is <see cref="MenuTier.Price"/>, which is shared by every option
    /// of that tier. Leaving the column empty removes any chance of an option
    /// drifting away from the tier price.
    /// </para>
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// The price tier this option belongs to. Null for combos that are not
    /// part of a tiered menu.
    /// </summary>
    public int? MenuTierId { get; set; }

    /// <summary>
    /// The option code inside its tier ("A", "B" or "C"). Null for combos
    /// that are not part of a tiered menu.
    /// </summary>
    public string? OptionCode { get; set; }

    /// <summary>
    /// Display order of this option inside its price tier (1 = A, 2 = B, 3 = C).
    /// </summary>
    public int OptionSortOrder { get; set; }

    /// <summary>Suggested guest count served by this combo, if defined.</summary>
    public int? GuestCount { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public MenuSet? MenuSet { get; set; }

    public MenuTier? MenuTier { get; set; }

    public ICollection<MenuComboDish> ComboDishes { get; set; } = new List<MenuComboDish>();
}
