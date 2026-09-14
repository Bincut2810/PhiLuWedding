namespace PhiluWedding.Domain.Enums;

/// <summary>
/// Lifecycle status of a customer booking consultation request.
/// </summary>
public enum BookingStatus
{
    /// <summary>New request, not yet reviewed.</summary>
    New = 0,

    /// <summary>Staff has contacted the customer.</summary>
    Contacted = 1,

    /// <summary>Booking has been confirmed.</summary>
    Confirmed = 2,

    /// <summary>Request was cancelled.</summary>
    Cancelled = 3
}
