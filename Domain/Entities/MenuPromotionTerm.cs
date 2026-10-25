namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A shared term that applies to every promotion tier (for example the rule
/// that promotions are counted from the tables written in the contract).
/// </summary>
public class MenuPromotionTerm
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>The term text, kept verbatim from the source sheet.</summary>
    public string Content { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
