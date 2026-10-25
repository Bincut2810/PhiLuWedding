namespace PhiluWedding.Domain;

/// <summary>
/// Shared vocabulary describing how a price is quoted in the source price sheet.
///
/// <para>
/// The source sheet mixes exact amounts, "from ...", ranges, "depends on the
/// chosen type", market dependent prices and rows without any price at all.
/// Flattening them into one nullable number would destroy information that
/// the public menu page has to render, so every price-bearing table stores
/// the quote kind next to the amount.
/// </para>
///
/// <para>
/// Vocabulary shared by <see cref="Entities.MenuService"/>,
/// <see cref="Entities.MenuAppetizerNut"/> and
/// <see cref="Entities.MenuBeverage"/> so the whole menu reads the same way.
/// </para>
/// </summary>
public static class MenuPriceTypes
{
    /// <summary>One exact amount.</summary>
    public const string Fixed = "Fixed";

    /// <summary>A starting amount ("từ ...").</summary>
    public const string From = "From";

    /// <summary>A minimum and a maximum amount.</summary>
    public const string Range = "Range";

    /// <summary>No fixed price — depends on the selected type of service.</summary>
    public const string DependsOnType = "DependsOnType";

    /// <summary>The price changes with market conditions and is set at the time of the event.</summary>
    public const string TimeDependent = "TimeDependent";

    /// <summary>The source states no price at all.</summary>
    public const string Unspecified = "Unspecified";

    /// <summary>True when <paramref name="value"/> is a recognised quote kind.</summary>
    public static bool IsKnown(string? value) =>
        value is Fixed or From or Range or DependsOnType or TimeDependent or Unspecified;
}
