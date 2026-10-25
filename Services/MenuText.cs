using System.Globalization;
using System.Text;

namespace PhiluWedding.Services;

/// <summary>
/// Text helpers shared by the menu seeders.
/// </summary>
internal static class MenuText
{
    /// <summary>
    /// Builds the comparison key used to recognise a dish that already exists
    /// in the global catalog. The key ignores case and collapses runs of
    /// whitespace, so the same dish written with different capitalisation
    /// ("Khai vị Tứ quý" / "Khai vị Tứ Quý") is treated as one dish.
    /// </summary>
    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    /// <summary>
    /// Builds an ASCII slug from a Vietnamese string. Vietnamese letters that
    /// carry no diacritic (<c>đ</c>, <c>ơ</c>, <c>ư</c>) are folded first,
    /// then the remaining letters are decomposed and the combining marks are
    /// dropped.
    /// </summary>
    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var folded = value
            .Replace("đ", "d", StringComparison.Ordinal)
            .Replace("Đ", "D", StringComparison.Ordinal)
            .Replace("ơ", "o", StringComparison.Ordinal)
            .Replace("Ơ", "O", StringComparison.Ordinal)
            .Replace("ư", "u", StringComparison.Ordinal)
            .Replace("Ư", "U", StringComparison.Ordinal);

        var builder = new StringBuilder(folded.Length);
        foreach (var ch in folded.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(ch);
                continue;
            }

            if (ch is >= 'A' and <= 'Z')
            {
                builder.Append(char.ToLowerInvariant(ch));
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '+' or '&' or ',' or '.' or '/' or '-' or '_')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
