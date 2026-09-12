using System;
using System.Collections.Generic;
using Jellyfin.Plugin.IMVDb.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.IMVDb;

/// <summary>
/// IMVDb Plugin.
/// </summary>
public class ImvdbPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the provider id.
    /// </summary>
    public const string ProviderName = "IMVDb";

    /// <summary>
    /// The provider id an item's IMVDb slug is stored under.
    /// </summary>
    public const string SlugProviderName = ProviderName + "_slug";

    /// <summary>
    /// The base url IMVDb serves its pages from.
    /// </summary>
    private const string WebBaseUrl = "https://imvdb.com/";

    /// <summary>
    /// The path entity pages live under.
    /// </summary>
    private const string EntityPathPrefix = "n/";

    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public ImvdbPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets current plugin instance.
    /// </summary>
    public static ImvdbPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "IMVDb";

    /// <inheritdoc />
    public override Guid Id => new Guid("A4967B35-15B3-46F0-BC7E-0B7D90623A85");

    /// <summary>
    /// Reduces an IMVDb page url to the slug stored as a provider id.
    /// </summary>
    /// <param name="pageUrl">The page url IMVDb reported.</param>
    /// <returns>The site relative path, or <c>null</c> when there is no usable one.</returns>
    public static string? GetSlugFromUrl(string? pageUrl)
    {
        if (string.IsNullOrEmpty(pageUrl)
            || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var slug = uri.AbsolutePath.Trim('/');

        return string.IsNullOrEmpty(slug) ? null : slug;
    }

    /// <summary>
    /// Builds the slug of an entity page from the entity slug a credit carries.
    /// </summary>
    /// <param name="entitySlug">The entity slug.</param>
    /// <returns>The site relative path of the entity's page.</returns>
    public static string GetEntitySlug(string entitySlug)
        => EntityPathPrefix + entitySlug;

    /// <summary>
    /// Builds the url of an IMVDb page from a stored slug.
    /// </summary>
    /// <param name="slug">The stored slug.</param>
    /// <returns>The page url, or <c>null</c> when the slug is unusable.</returns>
    public static string? GetPageUrl(string? slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return null;
        }

        // Refreshes before this stored the whole url rather than the path, so a stored value
        // that is already a url is reduced to its path first.
        if (Uri.TryCreate(slug, UriKind.Absolute, out var stored))
        {
            slug = stored.Scheme == Uri.UriSchemeHttp || stored.Scheme == Uri.UriSchemeHttps
                ? GetSlugFromUrl(slug)
                : null;

            if (string.IsNullOrEmpty(slug))
            {
                return null;
            }
        }

        return Uri.TryCreate(WebBaseUrl + slug.TrimStart('/'), UriKind.Absolute, out var pageUrl)
            ? pageUrl.AbsoluteUri
            : null;
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
        };
    }
}
