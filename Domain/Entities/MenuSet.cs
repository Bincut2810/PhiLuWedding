using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A high-level menu category (e.g. wedding menu, event menu, premium menu).
/// Names and slugs are intentionally generic here so real Phì Lũ naming can be applied later.
/// </summary>
public class MenuSet
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<MenuCombo> Combos { get; set; } = new List<MenuCombo>();
}
