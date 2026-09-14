using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A wedding banquet hall at Phì Lũ. The schema supports any number of halls;
/// the database is not hard-coded to four.
/// </summary>
public class WeddingHall
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public int? CapacityMin { get; set; }

    public int? CapacityMax { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<HallImage> Images { get; set; } = new List<HallImage>();
}
