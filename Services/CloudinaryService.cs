using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using PhiluWedding.Configuration;

namespace PhiluWedding.Services;

/// <summary>
/// Default <see cref="ICloudinaryService"/> implementation backed by the
/// official <c>CloudinaryDotNet</c> SDK.
///
/// Design notes:
///   * The underlying <see cref="Cloudinary"/> client is created once
///     (singleton-friendly) and only when all three credential fields
///     are present.
///   * The service never logs <c>ApiSecret</c> or signed upload payloads.
///     Error messages from Cloudinary are returned by the SDK and are
///     safe to log.
///   * The caller retains ownership of any <see cref="Stream"/> passed to
///     <see cref="UploadAsync"/>; this class does not dispose it.
/// </summary>
public sealed class CloudinaryService : ICloudinaryService
{
    private readonly ILogger<CloudinaryService> _logger;
    private readonly CloudinaryOptions _options;
    private readonly Cloudinary? _client;

    public CloudinaryService(IOptions<CloudinaryOptions> options, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        _options = options.Value;

        if (_options.IsConfigured)
        {
            var account = new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret);
            _client = new Cloudinary(account);
        }
        else
        {
            _logger.LogWarning(
                "Cloudinary is not configured (missing CloudName/ApiKey/ApiSecret). " +
                "Upload and delete calls will return a clear error until credentials are provided.");
        }
    }

    public bool IsConfigured => _client is not null;

    public async Task<CloudinaryUploadResult?> UploadAsync(
        Stream stream,
        string fileName,
        string? folder = null)
    {
        if (_client is null)
        {
            _logger.LogWarning("Cloudinary upload skipped: service is not configured.");
            return null;
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = string.IsNullOrWhiteSpace(folder) ? _options.Folder : folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _client.UploadAsync(uploadParams);

        if (result.Error is not null)
        {
            _logger.LogError(
                "Cloudinary upload failed for {FileName}: {Error}",
                fileName,
                result.Error.Message);
            return null;
        }

        if (string.IsNullOrWhiteSpace(result.PublicId) || result.SecureUrl is null)
        {
            _logger.LogError(
                "Cloudinary upload returned an empty publicId/secureUrl for {FileName}.",
                fileName);
            return null;
        }

        return new CloudinaryUploadResult(result.PublicId, result.SecureUrl.ToString());
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        if (_client is null)
        {
            _logger.LogWarning("Cloudinary delete skipped: service is not configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning("Cloudinary delete skipped: empty publicId.");
            return false;
        }

        var deleteParams = new DeletionParams(publicId);
        var result = await _client.DestroyAsync(deleteParams);

        if (result.Error is not null)
        {
            _logger.LogError(
                "Cloudinary delete failed for publicId '{PublicId}': {Error}",
                publicId,
                result.Error.Message);
            return false;
        }

        // The Cloudinary API returns the literal string "ok" on success
        // and "not found" when the asset is already gone. Both are
        // acceptable outcomes for an idempotent delete.
        return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase);
    }

    public string? BuildUrl(string? publicId, int? width = null, int? height = null)
    {
        if (_client is null || string.IsNullOrWhiteSpace(publicId))
        {
            return null;
        }

        var transformation = new Transformation().FetchFormat("auto").Quality("auto");

        if (width.HasValue)
        {
            transformation = transformation.Width(width.Value);
        }

        if (height.HasValue)
        {
            transformation = transformation.Height(height.Value);
        }

        return _client.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
    }
}