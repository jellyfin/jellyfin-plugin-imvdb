using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <summary>
/// IMVDb artist provider.
/// </summary>
public class ImvdbArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImvdbArtistProvider> _logger;
    private readonly IImvdbClient _imvdbClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbArtistProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ImvdbArtistProvider}"/> interface.</param>
    /// <param name="imvdbClient">Instance of the <see cref="IImvdbClient"/> interface.</param>
    public ImvdbArtistProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<ImvdbArtistProvider> logger,
        IImvdbClient imvdbClient)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _imvdbClient = imvdbClient;
    }

    /// <inheritdoc />
    public string Name
        => ImvdbPlugin.ProviderName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search result for {Name}", searchInfo.Name);

        var searchResults = await _imvdbClient.GetSearchResponseAsync(searchInfo, cancellationToken)
            .ConfigureAwait(false);
        if (searchResults == null)
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        return searchResults.Results.Select(
            r =>
            {
                var result = new RemoteSearchResult
                {
                    Name = r.Name
                };

                result.SetProviderId(ImvdbPlugin.ProviderName, r.Id.ToString(CultureInfo.InvariantCulture));

                return result;
            });
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get metadata result for {Name}", info.Name);
        var imvdbId = info.GetProviderId(ImvdbPlugin.ProviderName);
        var result = new MetadataResult<MusicArtist>
        {
            HasMetadata = false
        };

        // IMVDb id not provided, find first result.
        if (string.IsNullOrEmpty(imvdbId))
        {
            var searchResults = await GetSearchResults(info, cancellationToken)
                .ConfigureAwait(false);
            searchResults.FirstOrDefault()?.TryGetProviderId(ImvdbPlugin.ProviderName, out imvdbId);
        }

        // No results found, return without populating metadata.
        if (string.IsNullOrEmpty(imvdbId))
        {
            return result;
        }

        // do lookup here by imvdb id
        var releaseResult = await _imvdbClient.GetArtistIdResultAsync(imvdbId, cancellationToken)
            .ConfigureAwait(false);
        if (releaseResult != null)
        {
            result.HasMetadata = true;
            // set properties from data
            result.Item = new MusicArtist
            {
                Name = releaseResult.Name
            };

            result.Item.SetProviderId(ImvdbPlugin.ProviderName, imvdbId);

            if (!string.IsNullOrEmpty(releaseResult.Url))
            {
                result.Item.SetProviderId(ImvdbPlugin.ProviderName + "_slug", releaseResult.Url);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient(NamedClient.Default)
            .GetAsync(new Uri(url), cancellationToken);
    }
}
