using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PhiluWedding.Services;

/// <summary>
/// Strongly typed view of the version-controlled seed file
/// <c>Seed/menu-catalog.json</c>.
///
/// <para>
/// The JSON file is the single place where the fixed menu composition lives.
/// It is read at runtime from an embedded resource so the published Docker
/// image contains exactly the same data that is version controlled in the
/// repository.
/// </para>
/// </summary>
public sealed class MenuCatalogDocument
{
    [JsonPropertyName("menuSet")]
    public MenuSetDefinition? MenuSet { get; set; }

    [JsonPropertyName("tiers")]
    public List<MenuTierDefinition> Tiers { get; set; } = new();

    [JsonPropertyName("services")]
    public List<MenuServiceDefinition> Services { get; set; } = new();

    [JsonPropertyName("beverages")]
    public List<MenuBeverageDefinition> Beverages { get; set; } = new();

    [JsonPropertyName("nuts")]
    public List<MenuNutDefinition> Nuts { get; set; } = new();

    [JsonPropertyName("promotions")]
    public List<MenuPromotionDefinition> Promotions { get; set; } = new();

    [JsonPropertyName("promotionTerms")]
    public List<MenuPromotionTermDefinition> PromotionTerms { get; set; } = new();

    [JsonPropertyName("depositTerms")]
    public List<MenuDepositTermDefinition> DepositTerms { get; set; } = new();

    [JsonPropertyName("priceNotes")]
    public List<MenuPriceNoteDefinition> PriceNotes { get; set; } = new();
}

/// <summary>The wedding menu set the catalog belongs to.</summary>
public sealed class MenuSetDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("venueScope")]
    public string VenueScope { get; set; } = Domain.MenuVenueScopes.BothVenues;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>One price tier and the menu options sold at that price.</summary>
public sealed class MenuTierDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("guestsPerTable")]
    public int GuestsPerTable { get; set; } = 10;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("venueScope")]
    public string VenueScope { get; set; } = Domain.MenuVenueScopes.BothVenues;

    [JsonPropertyName("options")]
    public List<MenuOptionDefinition> Options { get; set; } = new();
}

/// <summary>One menu option (A, B or C) of a price tier.</summary>
public sealed class MenuOptionDefinition
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The fixed, ordered dish list of this option.</summary>
    [JsonPropertyName("dishes")]
    public List<string> Dishes { get; set; } = new();
}

/// <summary>An additional service or fee.</summary>
public sealed class MenuServiceDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("priceType")]
    public string PriceType { get; set; } = Domain.MenuPriceTypes.Unspecified;

    [JsonPropertyName("priceFrom")]
    public decimal? PriceFrom { get; set; }

    [JsonPropertyName("priceTo")]
    public decimal? PriceTo { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>A beverage sold by the unit.</summary>
public sealed class MenuBeverageDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("priceBelowThreshold")]
    public decimal? PriceBelowThreshold { get; set; }

    [JsonPropertyName("priceAboveThreshold")]
    public decimal? PriceAboveThreshold { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>An appetizer nut.</summary>
public sealed class MenuNutDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("priceType")]
    public string PriceType { get; set; } = Domain.MenuPriceTypes.Unspecified;

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>A promotion tier and the benefits it adds.</summary>
public sealed class MenuPromotionDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rangeLabel")]
    public string RangeLabel { get; set; } = string.Empty;

    [JsonPropertyName("minTableCount")]
    public int? MinTableCount { get; set; }

    [JsonPropertyName("maxTableCount")]
    public int? MaxTableCount { get; set; }

    /// <summary>Key of the tier this one inherits its benefits from.</summary>
    [JsonPropertyName("baseKey")]
    public string? BaseKey { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    /// <summary>The benefits this tier adds on top of the inherited tier.</summary>
    [JsonPropertyName("benefits")]
    public List<string> Benefits { get; set; } = new();
}

/// <summary>A term that applies to every promotion tier.</summary>
public sealed class MenuPromotionTermDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>A deposit milestone.</summary>
public sealed class MenuDepositTermDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("venueScope")]
    public string VenueScope { get; set; } = Domain.MenuVenueScopes.PhiLu;

    [JsonPropertyName("hallSlug")]
    public string? HallSlug { get; set; }

    [JsonPropertyName("hallName")]
    public string? HallName { get; set; }

    [JsonPropertyName("stage")]
    public string Stage { get; set; } = nameof(Domain.Enums.DepositStage.First);

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

/// <summary>Price metadata (provisional status, validity, VAT).</summary>
public sealed class MenuPriceNoteDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("validUntil")]
    public DateOnly? ValidUntil { get; set; }

    [JsonPropertyName("vatNote")]
    public string? VatNote { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}
