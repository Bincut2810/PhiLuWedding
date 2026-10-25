using System.Globalization;
using PhiluWedding.Domain;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Services;

/// <summary>
/// Formats the money shown on the public menu pages.
///
/// <para>
/// The menu catalog keeps the exact wording of the source price sheet: some
/// rows carry one amount, some a starting amount, some a range, some no
/// amount at all. This formatter preserves those kinds instead of flattening
/// them into a single number, so the public page never turns "no price" into
/// "0đ" and never turns a "from" price into an exact one.
/// </para>
///
/// <para>
/// Amounts are rendered with the Vietnamese group separator (a dot), so
/// 2,200,000 VND reads as <c>2.200.000đ</c>.
/// </para>
/// </summary>
public static class MenuPriceFormatter
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>The currency suffix used on the public pages.</summary>
    public const string CurrencySuffix = "đ";

    /// <summary>
    /// True when a stored amount may be shown as a price. A missing amount
    /// and an amount of zero both mean "no price" — never "0đ".
    /// </summary>
    public static bool IsDisplayable(decimal? amount) =>
        amount.HasValue && amount.Value > 0m;

    /// <summary>
    /// Formats an exact VND amount, for example 2200000 becomes
    /// <c>2.200.000đ</c>.
    /// </summary>
    public static string FormatAmount(decimal amount) =>
        amount.ToString("N0", VietnameseCulture) + CurrencySuffix;

    /// <summary>
    /// Formats an optional VND amount. Returns null when there is no price
    /// to show, so the caller can fall back to the source wording instead of
    /// printing an empty or zero price.
    /// </summary>
    public static string? FormatAmount(decimal? amount) =>
        IsDisplayable(amount) ? FormatAmount(amount!.Value) : null;

    /// <summary>
    /// Formats the price of an additional service according to the quote
    /// kind stored with the row.
    /// </summary>
    /// <returns>
    /// The price text, or null when the row has no numeric price (for
    /// example "tùy thuộc vào loại" or "chưa có giá"). Use
    /// <see cref="DescribeUnpriced"/> for those rows.
    /// </returns>
    public static string? FormatServicePrice(MenuService service)
    {
        switch (service.PriceType)
        {
            case MenuPriceTypes.Fixed:
            {
                var amount = FormatAmount(service.PriceFrom);
                return amount is null ? null : AppendUnit(amount, service.Unit);
            }

            case MenuPriceTypes.From:
            {
                var amount = FormatAmount(service.PriceFrom);
                return amount is null ? null : "Từ " + AppendUnit(amount, service.Unit);
            }

            case MenuPriceTypes.Range:
            {
                var from = FormatAmount(service.PriceFrom);
                var to = FormatAmount(service.PriceTo);

                if (from is null && to is null)
                {
                    return null;
                }

                if (from is null)
                {
                    return to;
                }

                return to is null ? from : $"{from} – {to}";
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// Formats the price of an appetizer nut. Only a fixed amount can be
    /// shown; a market-dependent or unstated price has no number at all.
    /// </summary>
    public static string? FormatAppetizerNutPrice(MenuAppetizerNut nut)
    {
        if (!string.Equals(nut.PriceType, MenuPriceTypes.Fixed, StringComparison.Ordinal))
        {
            return null;
        }

        var amount = FormatAmount(nut.Price);
        return amount is null ? null : AppendUnit(amount, nut.Unit);
    }

    /// <summary>
    /// Formats an optional amount and appends the unit when the source
    /// states one, for example <c>17.500đ / lon</c>.
    /// </summary>
    /// <param name="amount">The amount to format, in VND.</param>
    /// <param name="unit">The unit, when the source states one.</param>
    /// <returns>
    /// The price text, or null when there is no usable amount.
    /// </returns>
    public static string? FormatAmountWithUnit(decimal? amount, string? unit)
    {
        var formatted = FormatAmount(amount);
        return formatted is null ? null : AppendUnit(formatted, unit);
    }

    /// <summary>
    /// The wording shown for a row that has no numeric price.
    /// </summary>
    /// <param name="priceType">One of the <see cref="MenuPriceTypes"/> values.</param>
    /// <param name="note">The note from the source sheet, when it has one.</param>
    /// <remarks>
    /// The source note always wins, so the page never repeats a phrase the
    /// catalog already carries. Only when the source says nothing at all is
    /// a neutral placeholder used.
    /// </remarks>
    public static string DescribeUnpriced(string? priceType, string? note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            return note.Trim();
        }

        return priceType switch
        {
            MenuPriceTypes.DependsOnType => "Giá tùy thuộc vào loại được chọn.",
            MenuPriceTypes.TimeDependent => "Giá theo giá thị trường hiện tại.",
            _ => "Liên hệ nhà hàng để biết giá."
        };
    }

    /// <summary>
    /// Appends a unit to a formatted amount, for example
    /// <c>200.000đ / bàn</c>. The unit is only used when the source states
    /// one.
    /// </summary>
    private static string AppendUnit(string amount, string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return amount;
        }

        return $"{amount} / {unit.Trim()}";
    }
}
