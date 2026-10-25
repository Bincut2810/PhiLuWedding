namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A note about the menu prices themselves — provisional status, validity
/// period, VAT treatment.
///
/// <para>
/// The rows are the reusable metadata source for both the future public menu
/// page and the future admin price screen, so the wording is stored once in
/// the database instead of being hard-coded in a view.
/// </para>
/// </summary>
public class MenuPriceNote
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>The display text shown to customers and to administrators.</summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Last date the source prices were valid. The seeder never extends this
    /// date; the source validity ended in August 2026.
    /// </summary>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>How VAT is treated, when the source states it.</summary>
    public string? VatNote { get; set; }

    public string? Note { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
