using System.Collections.Generic;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A wedding banquet hall at Phì Lũ. The schema supports any number of halls;
/// the database is not hard-coded to four.
///
/// <para>
/// <see cref="VenueKey"/> groups a hall under a wedding venue (for example
/// <c>GoldenPhoenix</c> or <c>PhiLu</c>). When this is <c>null</c> the hall
/// is treated as a venue-less legacy row (e.g. an unassigned demo hall) and
/// is filtered out of the public venue-based flow.
/// </para>
/// </summary>
public class WeddingHall
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>
    /// Stable identifier of the wedding venue this hall belongs to.
    /// Allowed values are <c>GoldenPhoenix</c> and <c>PhiLu</c>.
    /// Null means the hall is unassigned (legacy or venue-less demo data)
    /// and is hidden from the public venue-based flow.
    /// </summary>
    public string? VenueKey { get; set; }

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
