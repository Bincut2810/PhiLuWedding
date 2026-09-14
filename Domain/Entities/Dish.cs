using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A single dish (món ăn). Reusable across many menu combos.
/// </summary>
public class Dish
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Vietnamese-friendly ingredient list. Stored as text in PostgreSQL.</summary>
    public string? Ingredients { get; set; }

    /// <summary>Reference price in VND. Optional because not every dish has a public price.</summary>
    public decimal? Price { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<DishImage> Images { get; set; } = new List<DishImage>();

    public ICollection<MenuComboDish> ComboDishes { get; set; } = new List<MenuComboDish>();
}
