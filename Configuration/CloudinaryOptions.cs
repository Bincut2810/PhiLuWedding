namespace PhiluWedding.Configuration;

/// <summary>
/// Strongly-typed configuration for the Cloudinary media service.
///
/// Values are loaded from the <c>Cloudinary</c> configuration section
/// (e.g. <c>appsettings.json</c>) and can be overridden via environment
/// variables such as <c>Cloudinary__CloudName</c>, <c>Cloudinary__ApiKey</c>,
/// <c>Cloudinary__ApiSecret</c>, <c>Cloudinary__Folder</c>.
///
/// Security rules:
///   * Never commit real credentials to source control.
///   * Leave values empty in committed configuration files.
///   * Provide real credentials via environment variables in Development
///     and via the deployment platform's secret store in Production.
/// </summary>
public class CloudinaryOptions
{
    /// <summary>Name of the configuration section bound to this class.</summary>
    public const string SectionName = "Cloudinary";

    /// <summary>The Cloudinary cloud name assigned to the account.</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>The Cloudinary API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The Cloudinary API secret. Must never be logged or exposed.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Default folder used when an upload does not specify one.</summary>
    public string Folder { get; set; } = "philu-wedding";

    /// <summary>
    /// Returns <c>true</c> when the three credential fields are all
    /// populated. The application is allowed to start even when this is
    /// <c>false</c>; in that case the Cloudinary service degrades to
    /// "not configured" and upload/delete calls return a clear error
    /// rather than crashing the host.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}