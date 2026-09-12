using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <summary>
/// External id for an IMVDb artist.
/// </summary>
public class ImvdbArtistExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public string Key
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public ExternalIdMediaType? Type
        => ExternalIdMediaType.Artist;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
        => item is MusicArtist;
}
