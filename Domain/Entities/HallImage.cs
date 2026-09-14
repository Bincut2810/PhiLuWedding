namespace PhiluWedding.Domain.Entities;

/// <summary>
/// An image belonging to a <see cref="WeddingHall"/>.
/// Cloudinary integration is not yet implemented; <see cref="PublicId"/> is reserved for a later phase.
/// </summary>
public class HallImage
{
    public int Id { get; set; }

    public int WeddingHallId { get; set; }

    public string ImageUrl { get; set; } = null!;

    /// <summary>Reserved for the future Cloudinary public id.</summary>
    public string? PublicId { get; set; }

    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public WeddingHall? WeddingHall { get; set; }
}
