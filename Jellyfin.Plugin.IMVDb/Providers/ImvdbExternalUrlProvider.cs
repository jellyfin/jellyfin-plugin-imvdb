using System;
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
        ArgumentNullException.ThrowIfNull(item);

        switch (item)
        {
            case MusicVideo:
            case MusicArtist:
            case Person:
                break;
            default:
                yield break;
        }

        if (item.TryGetProviderId(ImvdbPlugin.SlugProviderName, out var slug)
            && ImvdbPlugin.GetPageUrl(slug) is { } pageUrl)
        {
            yield return pageUrl;
        }
    }
}
