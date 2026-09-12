using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <summary>
/// External id for an IMVDb person, such as a credited director.
/// </summary>
public class ImvdbPersonExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public string Key
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public ExternalIdMediaType? Type
        => ExternalIdMediaType.Person;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
        => item is Person;
}
