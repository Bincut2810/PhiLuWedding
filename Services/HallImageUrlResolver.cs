using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Services;

/// <summary>
/// Resolves a delivery URL for a <see cref="HallImage"/> using a small,
/// opinionated priority list. Used by the public Razor Pages so the same
/// rule applies everywhere.
///
/// Priority:
///   1. Cloudinary (only when the service is configured AND the image
///      carries a non-empty <c>PublicId</c>).
///   2. The stored <c>ImageUrl</c>.
///
/// The resolver never fabricates a Cloudinary public id, never inserts
/// fake image rows, and never exposes Cloudinary credentials.
/// </summary>
public interface IHallImageUrlResolver
{
    /// <summary>
    /// Returns the best public delivery URL for the image, or <c>null</c>
    /// when neither Cloudinary nor the stored URL can serve it.
    /// </summary>
    string? Resolve(HallImage? image, int? width = null, int? height = null);
}

public sealed class HallImageUrlResolver : IHallImageUrlResolver
{
    private readonly ICloudinaryService _cloudinary;

    public HallImageUrlResolver(ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public string? Resolve(HallImage? image, int? width = null, int? height = null)
    {
        if (image is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(image.PublicId) && _cloudinary.IsConfigured)
        {
            var cloudinaryUrl = _cloudinary.BuildUrl(image.PublicId, width, height);
            if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
            {
                return cloudinaryUrl;
            }
        }

        return string.IsNullOrWhiteSpace(image.ImageUrl) ? null : image.ImageUrl;
    }
}