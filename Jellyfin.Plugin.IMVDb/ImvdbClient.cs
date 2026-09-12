using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.IMVDb.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb;

/// <summary>
/// The IMVDb client.
/// </summary>
public class ImvdbClient : IImvdbClient
{
    /// <summary>
    /// How long an API response is reused for when the configuration does not say.
    /// </summary>
    internal const int DefaultCacheDurationHours = 24;

    private const string BaseUrl = "https://imvdb.com/api/v1";

    /// <summary>
    /// The header IMVDb authenticates an application with.
    /// </summary>
    private const string ApiKeyHeader = "IMVDB-APP-KEY";

    /// <summary>
    /// Where a user goes to register an application and get a key.
    /// </summary>
    private const string ApiKeyRegistrationUrl = "https://imvdb.com/developers/apps/new";

    /// <summary>
    /// How many results to ask a search for. The endpoints accept up to 50, but the providers
    /// only ever offer the first handful to the user, so a smaller page keeps the responses -
    /// and the cache - small.
    /// </summary>
    private const int SearchPerPage = 25;

    /// <summary>
    /// How many pages of a search will ever be walked. Nothing that matches a library item is
    /// this far down the results, so the ceiling only serves to stop a response that reports a
    /// page count it never runs out of from being followed forever.
    /// </summary>
    private const int MaxSearchPages = 10;

    /// <summary>
    /// A response that failed outright is worth retrying sooner than a good one is worth
    /// refetching, but not immediately: a broken id or a dead endpoint would otherwise be asked
    /// for again on every single scan of the library.
    /// </summary>
    private static readonly TimeSpan _notFoundCacheDuration = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions _jsonSerializerOptions = JsonDefaults.Options;

    /// <summary>
    /// Whether the missing API key has already been reported. Client instances are short lived,
    /// so this outlives them.
    /// </summary>
    private static int _missingApiKeyLogged;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ImvdbClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="memoryCache">Instance of the <see cref="IMemoryCache"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ImvdbClient}"/> interface.</param>
    public ImvdbClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        ILogger<ImvdbClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ImvdbVideo> GetVideoSearchResultsAsync(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(searchInfo);

        var query = new StringBuilder(searchInfo.Name);
        if (searchInfo.Artists is not null)
        {
            foreach (var artist in searchInfo.Artists)
            {
                query.Append(' ')
                    .Append(artist);
            }
        }

        return EnumerateSearchResultsAsync<ImvdbVideo>("videos", query.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ImvdbArtist> GetArtistSearchResultsAsync(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(searchInfo);

        return EnumerateSearchResultsAsync<ImvdbArtist>("entities", searchInfo.Name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImvdbVideo?> GetVideoIdResultAsync(string imvdbId, CancellationToken cancellationToken)
    {
        // Credits are not part of the base video object and have to be asked for explicitly.
        var url = $"{BaseUrl}/video/{Uri.EscapeDataString(imvdbId)}?include=credits";
        return await GetResponseAsync<ImvdbVideo>(url, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ImvdbArtist?> GetArtistIdResultAsync(string imvdbId, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/entity/{Uri.EscapeDataString(imvdbId)}";
        return await GetResponseAsync<ImvdbArtist>(url, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildSearchUrl(string type, string? query, int page)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{BaseUrl}/search/{type}?q={Uri.EscapeDataString(query ?? string.Empty)}&per_page={SearchPerPage}&page={page}");

    /// <summary>
    /// Walks the pages of a search, yielding each page's results before asking for the next one.
    /// </summary>
    /// <remarks>
    /// The enumeration is lazy, so a caller that only wants the best match costs a single
    /// request: the page it stops on is the last one fetched. Each page is a separate url, so
    /// each is rate limited and cached in its own right.
    /// </remarks>
    private async IAsyncEnumerable<T> EnumerateSearchResultsAsync<T>(
        string type,
        string? query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var page = 1; page <= MaxSearchPages; page++)
        {
            var url = BuildSearchUrl(type, query, page);
            var response = await GetResponseAsync<ImvdbSearchResponse<T>>(url, cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
            {
                yield break;
            }

            foreach (var result in response.Results)
            {
                yield return result;
            }

            if (!response.HasNextPage)
            {
                yield break;
            }
        }

        _logger.LogDebug("Stopped walking the {Type} search for {Query} at the {MaxPages} page ceiling.", type, query, MaxSearchPages);
    }

    private async Task<T?> GetResponseAsync<T>(string url, CancellationToken cancellationToken)
    {
        // Without a key there is nothing this plugin can do: IMVDb answers an unauthenticated
        // request with HTTP 403, so the lookup is skipped rather than sent. The check comes
        // before the cache so that an unconfigured plugin behaves the same way throughout.
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return default;
        }

        // The url is the whole of the request, so it identifies the response on its own.
        if (_memoryCache.TryGetValue(url, out T? cached))
        {
            _logger.LogDebug("Serving {Url} from the cache.", url);
            return cached;
        }

        var (result, cacheDuration) = await FetchAsync<T>(url, apiKey, cancellationToken)
            .ConfigureAwait(false);

        if (cacheDuration > TimeSpan.Zero)
        {
            _memoryCache.Set(url, result, cacheDuration);
        }

        return result;
    }

    /// <summary>
    /// Sends the request, waiting for a rate limit slot first and retrying once if IMVDb rate
    /// limits anyway.
    /// </summary>
    /// <returns>
    /// The deserialized response, and how long it may be cached for -
    /// <see cref="TimeSpan.Zero"/> when it must not be.
    /// </returns>
    private async Task<(T? Result, TimeSpan CacheDuration)> FetchAsync<T>(string url, string apiKey, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await ImvdbRateLimiter.WaitForRequestSlot(_logger, cancellationToken).ConfigureAwait(false);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.TryAddWithoutValidation(ApiKeyHeader, apiKey);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryDelay = ImvdbRateLimiter.RegisterRateLimited(response, _logger);
                _logger.LogInformation("Rate limited by the IMVDb API. Retrying after {RetryDelay} ms.", retryDelay.TotalMilliseconds);
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            ImvdbRateLimiter.RegisterResponse(response, _logger);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("IMVDb has nothing at {Url}.", url);

                // Remember the miss for a while so a rescan does not ask again immediately.
                return (default, _notFoundCacheDuration);
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogError("The IMVDb API rejected the configured API key: {Error}", error);
                }
                else
                {
                    _logger.LogError("The IMVDb API request for {Url} failed with HTTP {StatusCode}: {Error}", url, (int)response.StatusCode, error);
                }

                // A rejected key or a server side failure is worth retrying on the next scan.
                return (default, TimeSpan.Zero);
            }

            try
            {
                var result = await response.Content
                    .ReadFromJsonAsync<T>(_jsonSerializerOptions, cancellationToken)
                    .ConfigureAwait(false);

                return (result, GetCacheDuration());
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize the IMVDb response for {Url}.", url);
                return (default, TimeSpan.Zero);
            }
        }

        _logger.LogWarning("Gave up on {Url} after the IMVDb API rate limited the retry as well.", url);
        return (default, TimeSpan.Zero);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            // Errors come back as {"status": false, "error": "..."}.
            var error = await response.Content
                .ReadFromJsonAsync<ImvdbError>(_jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(error?.Error))
            {
                return error.Error;
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or HttpRequestException)
        {
            // Not every failure is answered with the documented error object; nginx in front of
            // the API serves plain HTML for some of them.
        }

        return response.ReasonPhrase ?? string.Empty;
    }

    /// <summary>
    /// How long a good response may be reused for.
    /// </summary>
    private static TimeSpan GetCacheDuration()
    {
        var hours = ImvdbPlugin.Instance?.Configuration.CacheDurationHours ?? DefaultCacheDurationHours;

        // IMVDb asks that applications cache instead of requesting a bigger allowance, so
        // caching is never turned off entirely.
        return TimeSpan.FromHours(hours > 0 ? hours : DefaultCacheDurationHours);
    }

    private string? GetApiKey()
    {
        var apiKey = ImvdbPlugin.Instance?.Configuration.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Arm the error again, so that clearing the key later is reported afresh.
            Interlocked.Exchange(ref _missingApiKeyLogged, 0);

            return apiKey;
        }

        // A library scan asks for metadata once per item, so this is reported once rather than
        // once per item. It is armed again as soon as a key is configured.
        if (Interlocked.Exchange(ref _missingApiKeyLogged, 1) == 0)
        {
            _logger.LogError(
                "No IMVDb API key is configured, skipping all IMVDb lookups. Register an application at {Url} and enter its key on the IMVDb plugin's configuration page.",
                ApiKeyRegistrationUrl);
        }

        return null;
    }
}
