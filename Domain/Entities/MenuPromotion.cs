namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A promotion tier for wedding banquets.
///
/// <para>
/// Each row stores only the benefits that are <em>additional</em> to the
/// tier it inherits from, so the full benefit list is reconstructed
/// deterministically by walking <see cref="BasePromotionId"/> upwards. The
/// chain always points to a lower table-count tier, therefore the cumulative
/// list is unambiguous and never duplicated in the data.
/// </para>
/// </summary>
public class MenuPromotion
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>The table range as written in the source sheet, for example "14–24 bàn".</summary>
    public string RangeLabel { get; set; } = null!;

    /// <summary>Lowest table count of the tier. Null only when the tier is open-ended from the start.</summary>
    public int? MinTableCount { get; set; }

    /// <summary>Highest table count of the tier. Null for open-ended tiers ("Trên 95 bàn").</summary>
    public int? MaxTableCount { get; set; }

    /// <summary>
    /// The benefits this tier adds on top of the tier it inherits from. The
    /// inherited benefits are not copied into this row.
    /// </summary>
    public ICollection<MenuPromotionBenefit> Benefits { get; set; } = new List<MenuPromotionBenefit>();

    /// <summary>
    /// The tier this one inherits its benefits from. Null for the first tier.
    /// </summary>
    public int? BasePromotionId { get; set; }

    /// <summary>
    /// True when this tier includes the benefits of <see cref="BasePromotionId"/>.
    /// It is false for the first tier, which inherits from no other tier.
    /// </summary>
    public bool IncludesPreviousTier { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.PhiLu;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>The tier this one inherits from.</summary>
    public MenuPromotion? BasePromotion { get; set; }

    /// <summary>The tiers that inherit from this one.</summary>
    public ICollection<MenuPromotion> DerivedPromotions { get; set; } = new List<MenuPromotion>();
}
