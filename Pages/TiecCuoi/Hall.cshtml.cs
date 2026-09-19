using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PhiluWedding.Services;

namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Slug-based hall detail page backed by <see cref="StaticVenueCatalog"/>.
/// Used when the database is unavailable or has no matching row — keeps the
/// public hall detail accessible without requiring PostgreSQL.
///
/// <para>
/// URL: <c>/tiec-cuoi/hall/{slug}</c> (e.g. <c>/tiec-cuoi/hall/sanh-amber</c>).
/// </para>
/// </summary>
public class HallModel : PageModel
{
    public HallViewModel? Hall { get; private set; }

    public string BackHref { get; private set; } = "/TiecCuoi";

    public IActionResult OnGet(string slug)
    {
        var record = StaticVenueCatalog.FindBySlug(slug);
        if (record is null)
        {
            return NotFound();
        }

        var venueName = StaticVenueCatalog.GetVenueDisplayName(record.VenueKey);

        Hall = new HallViewModel(
            Id: record.Id,
            Name: record.Name,
            Slug: record.Slug,
            ShortDescription: $"{record.Name} — {venueName}",
            Description: $"Thông tin chi tiết của {record.Name} đang được cập nhật. " +
                         $"Vui lòng liên hệ trực tiếp để được tư vấn về không gian và phương án tiệc phù hợp.",
            CapacityMin: record.CapacityMin,
            CapacityMax: record.CapacityMax,
            Images: new List<HallImageViewModel>
            {
                new(
                    Id: record.Id,
                    Url: record.ImageUrl,
                    AltText: record.ImageAlt,
                    IsPrimary: true)
            },
            IsFromStaticCatalog: true);

        BackHref = StaticVenueHrefResolver.ResolveForSlug(record.Slug);
        return Page();
    }
}
