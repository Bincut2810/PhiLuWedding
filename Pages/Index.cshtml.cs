using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhiluWedding.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<HallPreview> Halls { get; } = new List<HallPreview>
    {
        new HallPreview(
            "SẢNH 01",
            "Sảnh 01 — Sang trọng",
            "Sang trọng · Tinh tế · Đẳng cấp",
            "https://images.unsplash.com/photo-1519225421980-715cb0215aed?auto=format&fit=crop&w=900&q=80",
            "Sảnh tiệc cưới trang trí hoa trắng — hình minh họa tạm thời"),
        new HallPreview(
            "SẢNH 02",
            "Sảnh 02 — Tinh tế",
            "Sang trọng · Tinh tế · Đẳng cấp",
            "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=900&q=80",
            "Sảnh tiệc ánh sáng vàng dịu — hình minh họa tạm thời"),
        new HallPreview(
            "SẢNH 03",
            "Sảnh 03 — Ấm cúng",
            "Sang trọng · Tinh tế · Đẳng cấp",
            "https://images.unsplash.com/photo-1465495976277-4387d4b0e4a6?auto=format&fit=crop&w=900&q=80",
            "Sảnh tiệc bàn tròn hoa tươi — hình minh họa tạm thời"),
        new HallPreview(
            "SẢNH 04",
            "Sảnh 04 — Đẳng cấp",
            "Sang trọng · Tinh tế · Đẳng cấp",
            "https://images.unsplash.com/photo-1525772764200-be829a350797?auto=format&fit=crop&w=900&q=80",
            "Sảnh tiệc nến và rèm vải — hình minh họa tạm thời")
    };

    public void OnGet()
    {
    }

    public record HallPreview(
        string Name,
        string ShortTitle,
        string Description,
        string ImageUrl,
        string AltText);
}
