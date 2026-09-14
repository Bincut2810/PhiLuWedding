using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A priced menu combo inside a <see cref="MenuSet"/>.
/// </summary>
public class MenuCombo
{
    public int Id { get; set; }

    public int MenuSetId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Price in VND. Stored as numeric(18,2) in PostgreSQL.</summary>
    public decimal? Price { get; set; }

    /// <summary>Suggested guest count served by this combo, if defined.</summary>
    public int? GuestCount { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public MenuSet? MenuSet { get; set; }

    public ICollection<MenuComboDish> ComboDishes { get; set; } = new List<MenuComboDish>();
}
