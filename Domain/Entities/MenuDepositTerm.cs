using PhiluWedding.Domain;
using PhiluWedding.Domain.Enums;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A deposit milestone of a wedding banquet contract.
///
/// <para>
/// The source terms are hall specific: each Phì Lũ hall has its own first
/// and second deposit amount. The hall is referenced by its stable slug
/// (<see cref="HallSlug"/>) rather than by a database id, and the optional
/// <see cref="WeddingHallId"/> is resolved by the seeder through a lookup so
/// the row still works when the hall has not been created yet.
/// </para>
///
/// <para>
/// A term with a null <see cref="HallSlug"/> applies to the whole venue, which
/// is how the final-payment policy is stored (the source states it once for
/// every hall).
/// </para>
/// </summary>
public class MenuDepositTerm
{
    public int Id { get; set; }

    /// <summary>Stable natural key used by the seeder to match rows.</summary>
    public string Key { get; set; } = null!;

    /// <summary>Venue applicability — see <see cref="MenuVenueScopes"/>.</summary>
    public string VenueScope { get; set; } = MenuVenueScopes.PhiLu;

    /// <summary>Stable slug of the hall this term belongs to. Null for venue-wide terms.</summary>
    public string? HallSlug { get; set; }

    /// <summary>Display name of the hall, kept so the term reads correctly without a join.</summary>
    public string? HallName { get; set; }

    /// <summary>
    /// Resolved link to the <see cref="WeddingHall"/> row. Null when the hall
    /// does not exist in the database yet; the term stays valid either way.
    /// </summary>
    public int? WeddingHallId { get; set; }

    /// <summary>The milestone this term describes.</summary>
    public DepositStage Stage { get; set; }

    /// <summary>Amount in VND. Null for stages the source does not attach an amount to.</summary>
    public decimal? Amount { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public WeddingHall? WeddingHall { get; set; }
}
