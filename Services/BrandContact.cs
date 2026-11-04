using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace PhiluWedding.Services;

/// <summary>
/// Resolves the official contact channels of the restaurant from the
/// <c>Brand:Contact</c> configuration section.
///
/// <para>
/// The single source of truth for these values is the configuration
/// file (<c>appsettings.json</c>); the defaults below are the
/// fallback used when a key is missing, so a misconfigured deployment
/// still renders a working hotline, Facebook page and Zalo chat link
/// instead of empty links.
/// </para>
///
/// <para>
/// Both the floating contact bubbles and the public contact page
/// resolve their links through this helper, so the two can never
/// drift apart — a change in configuration always changes both.
/// </para>
/// </summary>
public static class BrandContact
{
    /// <summary>Name of the configuration section bound to the contact channels.</summary>
    public const string SectionName = "Brand:Contact";

    /// <summary>Fallback hotline number (the confirmed business number).</summary>
    public const string DefaultHotlinePhone = "0905434115";

    /// <summary>Fallback official Facebook page URL.</summary>
    public const string DefaultFacebookUrl = "https://www.facebook.com/share/1BqiX3HRck/";

    /// <summary>Fallback official Zalo phone number.</summary>
    public const string DefaultZaloPhone = "0989821022";

    /// <summary>
    /// Reads the contact channels from the configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The resolved contact channels.</returns>
    public static BrandContactInfo Resolve(IConfiguration? configuration)
    {
        var hotlinePhone = Read(configuration, "HotlinePhone", DefaultHotlinePhone);
        var facebookUrl = Read(configuration, "FacebookUrl", DefaultFacebookUrl);
        var zaloPhone = Read(configuration, "ZaloPhone", DefaultZaloPhone);

        // The tel: link carries the number without separators, so the
        // dialer reads it correctly on every device.
        var hotlineDigits = Digits(hotlinePhone);
        var hotlineTel = string.IsNullOrEmpty(hotlineDigits)
            ? "tel:"
            : $"tel:{hotlineDigits}";

        // Public Zalo contact URL format is https://zalo.me/<phone> with
        // no separators, matching what Zalo's mobile and web apps
        // recognize.
        var zaloDigits = Digits(zaloPhone);
        var zaloUrl = string.IsNullOrEmpty(zaloDigits)
            ? "https://zalo.me/"
            : $"https://zalo.me/{zaloDigits}";

        return new BrandContactInfo(
            HotlinePhone: hotlinePhone,
            HotlineTel: hotlineTel,
            FacebookUrl: facebookUrl,
            ZaloPhone: zaloPhone,
            ZaloUrl: zaloUrl);
    }

    private static string Read(IConfiguration? configuration, string key, string fallback)
    {
        var value = configuration?[$"{SectionName}:{key}"];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Digits(string value) =>
        new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
}

/// <summary>
/// The official contact channels of NHÀ HÀNG PHÌ LŨ, resolved from
/// the <c>Brand:Contact</c> configuration section.
/// </summary>
/// <param name="HotlinePhone">The hotline number, as displayed.</param>
/// <param name="HotlineTel">The <c>tel:</c> URI used by click-to-call links.</param>
/// <param name="FacebookUrl">The official Facebook page URL.</param>
/// <param name="ZaloPhone">The Zalo phone number.</param>
/// <param name="ZaloUrl">The official Zalo chat URL.</param>
public sealed record BrandContactInfo(
    string HotlinePhone,
    string HotlineTel,
    string FacebookUrl,
    string ZaloPhone,
    string ZaloUrl);
