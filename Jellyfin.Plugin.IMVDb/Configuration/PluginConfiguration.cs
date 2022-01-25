using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.IMVDb.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the IMVDb ApiKey.
    /// </summary>
    public string? ApiKey { get; set; }
}
