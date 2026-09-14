namespace PhiluWedding.Services;

/// <summary>
/// Result of a successful Cloudinary upload. <see cref="SecureUrl"/> and
/// <see cref="PublicId"/> are intended to be persisted into the
/// <c>HallImage</c> / <c>DishImage</c> database columns.
/// </summary>
/// <param name="PublicId">The Cloudinary public identifier of the asset.</param>
/// <param name="SecureUrl">The HTTPS delivery URL of the asset.</param>
public sealed record CloudinaryUploadResult(string PublicId, string SecureUrl);

/// <summary>
/// Abstraction over the Cloudinary media service. Razor Pages and the
/// future admin CMS depend on this interface rather than on the SDK
/// directly, so the underlying SDK can be replaced or mocked without
/// touching call sites.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// <c>true</c> when the service has credentials and is ready to call
    /// the Cloudinary API. <c>false</c> when credentials are missing;
    /// upload/delete calls are then short-circuited and return a clear
    /// error so callers can surface a meaningful message.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Uploads an image stream to Cloudinary. The caller retains ownership
    /// of <paramref name="stream"/>; this method does not dispose it.
    /// </summary>
    /// <param name="stream">Image content to upload.</param>
    /// <param name="fileName">Original file name, used by Cloudinary to derive the public id when no explicit id is set.</param>
    /// <param name="folder">Optional folder override. When null, <see cref="Configuration.CloudinaryOptions.Folder"/> is used.</param>
    /// <returns>The upload result, or <c>null</c> when the service is not configured or the upload failed.</returns>
    Task<CloudinaryUploadResult?> UploadAsync(
        Stream stream,
        string fileName,
        string? folder = null);

    /// <summary>
    /// Deletes an asset from Cloudinary using its public identifier.
    /// Returns <c>false</c> when the service is not configured, the
    /// identifier is empty, or Cloudinary reports an error.
    /// </summary>
    Task<bool> DeleteAsync(string publicId);

    /// <summary>
    /// Builds an optimised delivery URL for the asset identified by
    /// <paramref name="publicId"/>. Returns <c>null</c> when the service
    /// is not configured or no public id was supplied.
    /// </summary>
    /// <param name="publicId">Cloudinary public identifier.</param>
    /// <param name="width">Optional target width in pixels.</param>
    /// <param name="height">Optional target height in pixels.</param>
    string? BuildUrl(string? publicId, int? width = null, int? height = null);
}