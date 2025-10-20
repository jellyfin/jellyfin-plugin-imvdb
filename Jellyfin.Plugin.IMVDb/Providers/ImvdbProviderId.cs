using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <inheritdoc />
public class ImvdbProviderId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public string Key
        => ImvdbPlugin.ProviderName + "_slug";

    /// <inheritdoc />
    public ExternalIdMediaType? Type
        => ExternalIdMediaType.ReleaseGroup;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
        => item is MusicVideo
           || item is MusicArtist
           || item is Person;
}
