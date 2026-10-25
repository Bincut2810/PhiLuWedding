using Microsoft.AspNetCore.Mvc.RazorPages;
using PhiluWedding.Data;
using PhiluWedding.Domain;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public wedding-menu browsing page for the Trung tâm Hội nghị Tiệc cưới
/// Phì Lũ venue (<c>/thuc-don/philu</c>).
///
/// <para>
/// The page shares its whole presentation with
/// <see cref="MenuGoldenPhoenixModel"/>: both venues read the same eight
/// shared price tiers and the same option composition. The venue only
/// decides which supporting sections are in scope — the price tiers are
/// confirmed for both venues, the remaining sections come from the Phì Lũ
/// source price sheet until the business confirms otherwise.
/// </para>
/// </summary>
public class MenuPhiLuModel : MenuVenuePageModel
{
    public MenuPhiLuModel(
        ILogger<MenuPhiLuModel> logger,
        PhiluWeddingDbContext? db = null)
        : base(logger, db)
    {
    }

    protected override string VenueKey => VenueKeys.PhiLu;

    protected override string VenueDisplayName => "Trung tâm Hội nghị Tiệc cưới Phì Lũ";

    protected override string VenuePageName => "/ThucDon/PhiLu";

    protected override string OtherVenueDisplayName => "Golden Phoenix";

    protected override string OtherVenuePageName => "/ThucDon/GoldenPhoenix";

    protected override string VenueIntroText =>
        "Thực đơn tiệc cưới tại Trung tâm Hội nghị Tiệc cưới Phì Lũ. " +
        "Các mức giá và phương án món ăn dưới đây dùng chung với Golden Phoenix.";

    /// <summary>
    /// Loads the catalog. <paramref name="muc"/> is the slug (or the stable
    /// key) of the price tier the visitor selected.
    /// </summary>
    public async Task OnGetAsync(string? muc) => await LoadMenuAsync(muc);
}
