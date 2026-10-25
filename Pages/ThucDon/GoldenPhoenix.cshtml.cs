using Microsoft.AspNetCore.Mvc.RazorPages;
using PhiluWedding.Data;
using PhiluWedding.Domain;

namespace PhiluWedding.Pages.ThucDon;

/// <summary>
/// Public wedding-menu browsing page for the Golden Phoenix venue
/// (<c>/thuc-don/golden-phoenix</c>).
///
/// <para>
/// The page shares its whole presentation with
/// <see cref="MenuPhiLuModel"/>: both venues read the same eight shared
/// price tiers and the same option composition, so the price of a tier is
/// never duplicated per venue. The venue only decides which supporting
/// sections are in scope — the additional services, beverages, appetizer
/// nuts, promotions and deposit terms come from the Phì Lũ source price
/// sheet and stay scoped to Phì Lũ until the business confirms otherwise.
/// </para>
/// </summary>
public class MenuGoldenPhoenixModel : MenuVenuePageModel
{
    public MenuGoldenPhoenixModel(
        ILogger<MenuGoldenPhoenixModel> logger,
        PhiluWeddingDbContext? db = null)
        : base(logger, db)
    {
    }

    protected override string VenueKey => VenueKeys.GoldenPhoenix;

    protected override string VenueDisplayName => "Golden Phoenix";

    protected override string VenuePageName => "/ThucDon/GoldenPhoenix";

    protected override string OtherVenueDisplayName => "Trung tâm Hội nghị Tiệc cưới Phì Lũ";

    protected override string OtherVenuePageName => "/ThucDon/PhiLu";

    protected override string VenueIntroText =>
        "Thực đơn tiệc cưới tại Golden Phoenix. " +
        "Các mức giá và phương án món ăn dưới đây dùng chung với " +
        "Trung tâm Hội nghị Tiệc cưới Phì Lũ.";

    /// <summary>
    /// Loads the catalog. <paramref name="muc"/> is the slug (or the stable
    /// key) of the price tier the visitor selected.
    /// </summary>
    public async Task OnGetAsync(string? muc) => await LoadMenuAsync(muc);
}
