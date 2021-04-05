using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.IMVDb.Models;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb
{
    /// <summary>
    /// The IMVDb client.
    /// </summary>
    public class ImvdbClient : IImvdbClient
    {
        private const string BaseUrl = "https://imvdb.com/api/v1";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImvdbClient> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImvdbClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ImvdbClient}"/> interface.</param>
        public ImvdbClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ImvdbClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonSerializerOptions = JsonDefaults.GetOptions();
        }

        /// <inheritdoc />
        public async Task<ImvdbSearchResponse?> GetSearchResponseAsync(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/search/videos?q={string.Join("+", searchInfo.Artists)}+{searchInfo.Name}";
            await using var response = await GetResponseAsync(url, cancellationToken)
                .ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<ImvdbSearchResponse>(response, _jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ImvdbVideo?> GetIdResultAsync(string imvdbId, CancellationToken cancellationToken)
        {
            var apiKey = GetApiKey();
            if (apiKey == null)
            {
                return null;
            }

            var url = $"{BaseUrl}/video/{imvdbId}";
            await using var response = await GetResponseAsync(url, cancellationToken)
                .ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<ImvdbVideo>(response, _jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Stream> GetResponseAsync(string url, CancellationToken cancellationToken)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.TryAddWithoutValidation("IMVDB-APP-KEY", GetApiKey());
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private string? GetApiKey()
        {
            var apiKey = ImvdbPlugin.Instance?.Configuration.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("ApiKey is unset");
            }

            return apiKey;
        }
    }
}