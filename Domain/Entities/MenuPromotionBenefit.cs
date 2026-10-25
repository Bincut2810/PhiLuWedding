namespace PhiluWedding.Domain.Entities;

/// <summary>
/// One benefit granted by a promotion tier.
///
/// <para>
/// A promotion tier stores only the benefits it adds on top of the tier it
/// inherits from, so the inherited list is never duplicated in the data. The
/// full cumulative benefit list is reconstructed by walking the
/// <see cref="MenuPromotion.BasePromotion"/> chain from the highest tier down
/// and reading the benefits of each tier in sort order.
/// </para>
/// </summary>
public class MenuPromotionBenefit
{
    public int Id { get; set; }

    public int MenuPromotionId { get; set; }

    /// <summary>Order of the benefit inside its promotion tier.</summary>
    public int SortOrder { get; set; }

    /// <summary>The benefit text, kept close to the source wording.</summary>
    public string Text { get; set; } = null!;

    public MenuPromotion? MenuPromotion { get; set; }
}
