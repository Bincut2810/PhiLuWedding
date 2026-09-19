namespace PhiluWedding.Pages.TiecCuoi;

/// <summary>
/// Flat view model used by the hall detail partial. Lives outside any
/// single page model so that <c>Details</c> (database-backed) and
/// <c>Hall</c> (static-catalog-backed) can render the same template.
/// </summary>
public sealed record HallViewModel(
    int Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    int? CapacityMin,
    int? CapacityMax,
    IReadOnlyList<HallImageViewModel> Images,
    bool IsFromStaticCatalog);

/// <summary>Single image entry inside <see cref="HallViewModel.Images"/>.</summary>
public sealed record HallImageViewModel(
    int Id,
    string? Url,
    string? AltText,
    bool IsPrimary);
