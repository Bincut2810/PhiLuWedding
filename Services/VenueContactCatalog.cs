using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhiluWedding.Services;

/// <summary>
/// The two wedding venues of NHÀ HÀNG PHÌ LŨ together with their
/// confirmed public addresses and the map links built from them.
///
/// <para>
/// The addresses are the confirmed business truth — they are never
/// derived from coordinates. The Google Maps links are generated from
/// the address string itself, so no coordinate is ever invented:
///   <list type="bullet">
///     <item><c>maps.google.com/maps?q=&lt;address&gt;&amp;output=embed</c>
///           renders a map centred on the address (no API key needed).</item>
///     <item><c>google.com/maps/dir/?api=1&amp;destination=&lt;address&gt;</c>
///           opens the directions planner for that address.</item>
///   </list>
/// </para>
///
/// <para>
/// Image paths point at the public static assets under
/// <c>wwwroot/images/venues/...</c>, the same photography the venue
/// pages already use.
/// </para>
/// </summary>
public static class VenueContactCatalog
{
    /// <summary>All venues, in display order (Phì Lũ first, Golden Phoenix second).</summary>
    public static IReadOnlyList<VenueContact> Venues { get; } = new[]
    {
        new VenueContact(
            Key: "philu",
            Name: "Tiệc cưới Phì Lũ",
            Role: "Trung tâm Hội nghị Tiệc cưới",
            Address: "3 Đ. 2 Tháng 9, Hòa Cường, Đà Nẵng 550000, Việt Nam",
            ImageUrl: "/images/venues/philu/cover.webp",
            ImageAlt: "Sảnh đón khách của Trung tâm Hội nghị Tiệc cưới Phì Lũ",
            HallImageUrl: "/images/venues/philu/hall-crystal.webp",
            HallImageAlt: "Sảnh Crystal của Trung tâm Hội nghị Tiệc cưới Phì Lũ",
            DetailsUrl: "/tiec-cuoi/philu"),

        new VenueContact(
            Key: "golden-phoenix",
            Name: "Golden Phoenix",
            Role: "Nhà hàng tiệc cưới",
            Address: "A8.2 Lô 1, 2 Đ. 2 Tháng 9, Hòa Cường, Đà Nẵng 550000, Việt Nam",
            ImageUrl: "/images/venues/golden-phoenix/cover.webp",
            ImageAlt: "Khu đón khách của nhà hàng tiệc cưới Golden Phoenix",
            HallImageUrl: "/images/venues/golden-phoenix/hall-a.webp",
            HallImageAlt: "Sảnh A của nhà hàng tiệc cưới Golden Phoenix",
            DetailsUrl: "/tiec-cuoi/golden-phoenix"),
    };

    /// <summary>
    /// Builds the Google Maps embed URL for an address. The address is
    /// URL-encoded, so Vietnamese diacritics survive the round trip.
    /// </summary>
    public static string BuildEmbedUrl(string address) =>
        $"https://maps.google.com/maps?q={Uri.EscapeDataString(address)}&hl=vi&z=16&output=embed";

    /// <summary>
    /// Builds the Google Maps directions URL for an address.
    /// </summary>
    public static string BuildDirectionsUrl(string address) =>
        $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(address)}";

    /// <summary>
    /// Builds the Google Maps place-search URL for an address — used by
    /// the "open in Google Maps" link of a venue card.
    /// </summary>
    public static string BuildPlaceUrl(string address) =>
        $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address)}";
}

/// <summary>
/// One venue's public contact data: its confirmed address, its
/// photography and the links built from that address.
/// </summary>
/// <param name="Key">Stable identifier of the venue.</param>
/// <param name="Name">The venue's public name.</param>
/// <param name="Role">The short descriptor shown above the name.</param>
/// <param name="Address">The confirmed public address.</param>
/// <param name="ImageUrl">Path of the venue's cover photograph.</param>
/// <param name="ImageAlt">Alternative text for the cover photograph.</param>
/// <param name="HallImageUrl">Path of a second, interior photograph.</param>
/// <param name="HallImageAlt">Alternative text for the interior photograph.</param>
/// <param name="DetailsUrl">Public route of the venue's hall listing.</param>
public sealed record VenueContact(
    string Key,
    string Name,
    string Role,
    string Address,
    string ImageUrl,
    string ImageAlt,
    string HallImageUrl,
    string HallImageAlt,
    string DetailsUrl)
{
    /// <summary>The Google Maps embed URL for this venue's address.</summary>
    public string MapEmbedUrl => VenueContactCatalog.BuildEmbedUrl(Address);

    /// <summary>The Google Maps directions URL for this venue's address.</summary>
    public string MapDirectionsUrl => VenueContactCatalog.BuildDirectionsUrl(Address);

    /// <summary>The Google Maps place-search URL for this venue's address.</summary>
    public string MapPlaceUrl => VenueContactCatalog.BuildPlaceUrl(Address);
}
