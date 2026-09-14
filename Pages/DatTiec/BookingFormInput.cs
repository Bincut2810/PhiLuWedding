using System.ComponentModel.DataAnnotations;

namespace PhiluWedding.Pages.DatTiec;

/// <summary>
/// View-model that the public booking form binds to. It is intentionally
/// a separate type from <c>BookingRequest</c> so that:
///   * database internals (Id, Status, CreatedAt, UpdatedAt) are never
///     reachable from the form,
///   * the visitor cannot set <c>BookingStatus</c> from the browser,
///   * validation rules live with the form definition.
/// </summary>
public class BookingFormInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(200, MinimumLength = 2,
        ErrorMessage = "Họ và tên phải có độ dài từ 2 đến 200 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(30, MinimumLength = 8,
        ErrorMessage = "Số điện thoại phải có độ dài từ 8 đến 30 ký tự.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày tổ chức dự kiến.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày tổ chức dự kiến")]
    public DateOnly EventDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại sự kiện.")]
    [StringLength(100, MinimumLength = 1,
        ErrorMessage = "Loại sự kiện phải có độ dài từ 1 đến 100 ký tự.")]
    [Display(Name = "Loại sự kiện")]
    public string EventType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số lượng khách dự kiến.")]
    [Range(1, 5000, ErrorMessage = "Số lượng khách phải nằm trong khoảng từ 1 đến 5.000.")]
    [Display(Name = "Số lượng khách dự kiến")]
    public int EstimatedGuestCount { get; set; }

    [StringLength(2000, ErrorMessage = "Ghi chú không được vượt quá 2.000 ký tự.")]
    [Display(Name = "Ghi chú thêm")]
    public string? Note { get; set; }

    /// <summary>
    /// Optional reference to a published <c>WeddingHall</c>. The value is
    /// bound from a hidden form field populated on the page; the page
    /// model re-validates it against the database so a tampered id is
    /// never persisted.
    /// </summary>
    [Display(Name = "Sảnh tiệc quan tâm")]
    public int? PreferredHallId { get; set; }
}