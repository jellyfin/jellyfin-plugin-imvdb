using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.IMVDb.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        RateLimit = ImvdbRateLimiter.DefaultRequestsPerMinute;
        CacheDurationHours = ImvdbClient.DefaultCacheDurationHours;
    }

    /// <summary>
    /// Gets or sets the IMVDb ApiKey.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets how many requests per minute may be sent to the IMVDb API. IMVDb documents a
    /// ceiling of <see cref="ImvdbRateLimiter.MaxRequestsPerMinute"/> calls per minute per
    /// application key and applies its own allowance on top of this, and the plugin never
    /// exceeds the lowest of the three. Zero or less falls back to
    /// <see cref="ImvdbRateLimiter.DefaultRequestsPerMinute"/>.
    /// </summary>
    public int RateLimit { get; set; }

    /// <summary>
    /// Gets or sets how many hours an API response is reused before it is fetched again. IMVDb
    /// asks that applications cache rather than request a higher rate limit, so caching cannot
    /// be turned off entirely; zero or less falls back to
    /// <see cref="ImvdbClient.DefaultCacheDurationHours"/>.
    /// </summary>
    public int CacheDurationHours { get; set; }
}
