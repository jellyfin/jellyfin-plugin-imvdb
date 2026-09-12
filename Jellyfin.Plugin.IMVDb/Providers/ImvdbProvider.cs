using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.IMVDb.Models;
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
    private const int MaxSearchResults = 50;

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

        // IMVDb id not provided, find first result. Enumerating lazily means only the first page
        // of the search is ever fetched.
        if (string.IsNullOrEmpty(imvdbId))
        {
            var bestMatch = await _imvdbClient.GetVideoSearchResultsAsync(info, cancellationToken)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            imvdbId = bestMatch?.Id.ToString(CultureInfo.InvariantCulture);
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
                Artists = releaseResult.Artists.Select(i => i.Name).ToArray()
            };

            if (!string.IsNullOrEmpty(releaseResult.Image?.Size1))
            {
                result.Item.ImageInfos = [new ItemImageInfo { Path = releaseResult.Image.Size1 }];
            }

            foreach (var credit in releaseResult.Credits?.Crew ?? Array.Empty<ImvdbCrewCredit>())
            {
                var personKind = GetPersonKind(credit.PositionCode);
                if (personKind is null)
                {
                    continue;
                }

                var providerIds = new Dictionary<string, string>
                {
                    { ImvdbPlugin.ProviderName, credit.Id.ToString(CultureInfo.InvariantCulture) }
                };

                // A credit carries the entity's slug rather than its page path, so the path is
                // built the same way IMVDb builds it.
                if (!string.IsNullOrEmpty(credit.Slug))
                {
                    providerIds[ImvdbPlugin.SlugProviderName] = ImvdbPlugin.GetEntitySlug(credit.Slug);
                }

                result.AddPerson(new PersonInfo
                {
                    Name = credit.Name,
                    ProviderIds = providerIds,
                    Type = personKind.Value
                });
            }

            result.Item.SetProviderId(ImvdbPlugin.ProviderName, imvdbId);

            var slug = ImvdbPlugin.GetSlugFromUrl(releaseResult.Url);
            if (!string.IsNullOrEmpty(slug))
            {
                result.Item.SetProviderId(ImvdbPlugin.SlugProviderName, slug);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get search result for {Name}", searchInfo.Name);

        // Jellyfin shows the caller every result a provider hands back, so the paging stops once
        // there are more candidates than anyone would pick from.
        return await _imvdbClient.GetVideoSearchResultsAsync(searchInfo, cancellationToken)
            .Take(MaxSearchResults)
            .Select(ToSearchResult)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient(NamedClient.Default)
            .GetAsync(new Uri(url), cancellationToken);
    }

    private static RemoteSearchResult ToSearchResult(ImvdbVideo video)
    {
        var result = new RemoteSearchResult
        {
            Name = video.SongTitle,
            ProductionYear = video.Year,
            Artists = video.Artists.Select(a => new RemoteSearchResult { Name = a.Name }).ToArray(),
            ImageUrl = video.Image?.Size1,
        };

        result.SetProviderId(ImvdbPlugin.ProviderName, video.Id.ToString(CultureInfo.InvariantCulture));

        return result;
    }

    private static PersonKind? GetPersonKind(string? positionCode)
        => positionCode switch
        {
            ImvdbCrewCredit.DirectorPositionCode => PersonKind.Director,
            ImvdbCrewCredit.ProducerPositionCode => PersonKind.Producer,
            _ => null
        };
}
