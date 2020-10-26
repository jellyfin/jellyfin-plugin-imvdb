using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.IMVDb.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb
{
    /// <summary>
    /// IMVDb Provider.
    /// </summary>
    public class ImvdbProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
    {
        private const string BaseUrl = "https://imvdb.com/api/v1";

        private readonly IHttpClient _httpClient;
        private readonly ILogger<ImvdbProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImvdbProvider"/> class.
        /// </summary>
        /// <param name="httpClient">Instance of the <see cref="IHttpClient"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ImvdbProvider}"/> interface.</param>
        public ImvdbProvider(IHttpClient httpClient, ILogger<ImvdbProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "IMVDb";

        /// <inheritdoc />
        public async Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Get metadata result for {Name}", info.Name);
            var imvdbId = info.GetProviderId(ImvdbPlugin.ProviderName);
            var result = new MetadataResult<MusicVideo>
            {
                HasMetadata = false
            };

            if (string.IsNullOrEmpty(imvdbId))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                var searchResult = searchResults.FirstOrDefault();
                if (searchResult != null)
                {
                    result.HasMetadata = true;
                    result.Item = new MusicVideo
                    {
                        Name = searchResult.Name,
                        ProductionYear = searchResult.ProductionYear,
                        Artists = searchResult.Artists.Select(i => i.Name).ToArray(),
                        ImageInfos = new[]
                        {
                            new ItemImageInfo
                            {
                                Path = searchResult.ImageUrl
                            }
                        }
                    };
                }
            }
            else
            {
                // do lookup here by imvdb id
                var releaseResult = await GetIdResult(imvdbId, cancellationToken).ConfigureAwait(false);
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
                    result.Item.SetProviderId(ImvdbPlugin.ProviderName, imvdbId);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Get search result for {Name}", searchInfo.Name);
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var searchResults = await GetSearchResponse(searchInfo, apiKey, cancellationToken).ConfigureAwait(false);
            return searchResults.Results.Select(
                r =>
                {
                    var result = new RemoteSearchResult
                    {
                        Name = r.SongTitle,
                        ProductionYear = r.Year,
                        Artists = r.Artists.Select(a => new RemoteSearchResult { Name = a.Name }).ToArray(),
                        ImageUrl = r.Image?.Size1
                    };

                    result.SetProviderId(ImvdbPlugin.ProviderName, r.Id.ToString(CultureInfo.InvariantCulture));

                    return result;
                });
        }

        /// <inheritdoc />
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        private string? GetApiKey()
        {
            var apiKey = ImvdbPlugin.Instance?.Configuration.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("ApiKey is unset.");
            }

            return apiKey;
        }

        private async Task<ImvdbVideo?> GetIdResult(string imvdbId, CancellationToken cancellationToken)
        {
            var apiKey = GetApiKey();
            if (apiKey == null)
            {
                return null;
            }

            var url = $"{BaseUrl}/video/{imvdbId}";
            using var response = await GetResponse(url, apiKey, cancellationToken).ConfigureAwait(false);
            await using var stream = response.Content;
            return await JsonSerializer.DeserializeAsync<ImvdbVideo>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImvdbSearchResponse> GetSearchResponse(MusicVideoInfo searchInfo, string apiKey, CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/search/videos?q={string.Join("+", searchInfo.Artists)}+{searchInfo.Name}";
            using var response = await GetResponse(url, apiKey, cancellationToken).ConfigureAwait(false);
            await using var stream = response.Content;
            return await JsonSerializer.DeserializeAsync<ImvdbSearchResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private Task<HttpResponseInfo> GetResponse(string url, string apiKey, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                AcceptHeader = "application/json",
                Url = url,
                RequestHeaders =
                {
                    ["IMVDB-APP-KEY"] = apiKey
                }
            };

            return _httpClient.GetResponse(options);
        }
    }
}
