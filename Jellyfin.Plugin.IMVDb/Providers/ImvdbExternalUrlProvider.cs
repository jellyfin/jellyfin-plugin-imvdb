using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <summary>
/// External url provider for IMVDB.
/// </summary>
public class ImvdbExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId(ImvdbPlugin.ProviderName + "_slug", out var externalId))
        {
            switch (item)
            {
                case MusicVideo:
                case MusicArtist:
                case Person:
                    // The external id is the entire url.
                    yield return externalId;
                    break;
            }
        }
    }
}
