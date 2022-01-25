using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb.Providers;

/// <summary>
/// IMVDb Provider.
/// </summary>
public class ImvdbProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImvdbProvider> _logger;
    private readonly IImvdbClient _imvdbClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ImvdbProvider}"/> interface.</param>
    /// <param name="imvdbClient">Instance of the <see cref="IImvdbClient"/> interface.</param>
    public ImvdbProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<ImvdbProvider> logger,
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
    public async Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get metadata result for {Name}", info.Name);
        var imvdbId = info.GetProviderId(ImvdbPlugin.ProviderName);
        var result = new MetadataResult<MusicVideo>
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
        var releaseResult = await _imvdbClient.GetVideoIdResultAsync(imvdbId, cancellationToken)
            .ConfigureAwait(false);
        if (releaseResult != null)
        {
            result.HasMetadata = true;
            // set properties from data
            result.Item = new MusicVideo
            {
                Name = releaseResult.SongTitle,
                ProductionYear = releaseResult.Year,
                Artists = releaseResult.Artists.Select(i => i.Name).ToArray(),
                ImageInfos = new[]
                {
                    new ItemImageInfo
                    {
                        Path = releaseResult.Image?.Size1
                    }
                }
            };

            foreach (var director in releaseResult.Directors)
            {
                result.AddPerson(new PersonInfo
                {
                    Name = director.Name,
                    ProviderIds = new Dictionary<string, string>
                    {
                        { ImvdbPlugin.ProviderName, director.Id.ToString(CultureInfo.InvariantCulture) },
                        { ImvdbPlugin.ProviderName + "_slug", director.Url },
                    },
                    Type = director.Position
                });
            }

            result.Item.SetProviderId(ImvdbPlugin.ProviderName, imvdbId);

            if (!string.IsNullOrEmpty(releaseResult.Url))
            {
                result.Item.SetProviderId(ImvdbPlugin.ProviderName + "_slug", releaseResult.Url);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
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
                    Name = r.SongTitle,
                    ProductionYear = r.Year,
                    Artists = r.Artists.Select(a => new RemoteSearchResult { Name = a.Name }).ToArray(),
                    ImageUrl = r.Image?.Size1,
                };

                result.SetProviderId(ImvdbPlugin.ProviderName, r.Id.ToString(CultureInfo.InvariantCulture));

                return result;
            });
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient(NamedClient.Default)
            .GetAsync(new Uri(url), cancellationToken);
    }
}
