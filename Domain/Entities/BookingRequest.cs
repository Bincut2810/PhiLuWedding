using PhiluWedding.Domain.Enums;

namespace PhiluWedding.Domain.Entities;

/// <summary>
/// A consultation request submitted by a prospective customer.
/// Email notifications, status workflows, and admin UI are NOT part of this phase.
/// </summary>
public class BookingRequest
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    /// <summary>The customer's event date. Stored as a calendar date.</summary>
    public DateOnly EventDate { get; set; }

    public string PhoneNumber { get; set; } = null!;

    /// <summary>Optional email address. Length and format are validated by the form layer later.</summary>
    public string? Email { get; set; }

    public string EventType { get; set; } = null!;

    public int EstimatedGuestCount { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Optional reference to the hall the customer is most interested in.
    /// Stored as a nullable FK so an existing request can be back-filled
    /// without forcing a value, and so an unpublished / deleted hall does
    /// not break historical rows.
    /// </summary>
    public int? PreferredHallId { get; set; }

    public WeddingHall? PreferredHall { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.New;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
