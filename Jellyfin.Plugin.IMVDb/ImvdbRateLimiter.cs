using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.IMVDb;

/// <summary>
/// Spaces out requests to the IMVDb API so that a library scan stays inside the documented
/// fair use limit (https://imvdb.com/developers/api).
///
/// The configured requests-per-minute is only an upper bound: should IMVDb report the allowance
/// it is applying through X-RateLimit-Limit and how much of it is left through
/// X-RateLimit-Remaining, the limiter follows both. The remaining count is what keeps the plugin
/// polite when it is not the only thing spending the allowance - the allowance belongs to the
/// application key, so a second Jellyfin server sharing the key, or any other application built
/// on it, eats into the same budget. Rather than racing ahead on its own schedule and
/// rediscovering the ceiling through HTTP 429s, the limiter spreads whatever is left over the
/// rest of the window, so a shared allowance slows the scan down instead of exhausting it.
/// </summary>
internal static class ImvdbRateLimiter
{
    /// <summary>
    /// The allowance assumed before IMVDb has reported one of its own. IMVDb documents a
    /// ceiling of 1000 calls per minute, and asks that applications cache rather than ask for
    /// more, so the default sits well below it: metadata scans are bursty and the allowance is
    /// shared by every install using the same application key.
    /// </summary>
    internal const int DefaultRequestsPerMinute = 120;

    /// <summary>
    /// The ceiling IMVDb documents for a single application key. A configured allowance is
    /// never allowed above this, whatever the configuration page was told.
    /// </summary>
    internal const int MaxRequestsPerMinute = 1000;

    /// <summary>
    /// The length of the allowance window IMVDb applies.
    /// </summary>
    private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Task.Delay may fire fractionally early, so a spin of the wait loop never sleeps
    /// for less than this.
    /// </summary>
    private static readonly TimeSpan _minimumDelay = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// How long to hold off for when IMVDb rate limits without saying for how long. The
    /// allowance is documented per minute, so a whole window is assumed to be lost.
    /// </summary>
    private static readonly TimeSpan _fallbackRetryDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Serializes callers so that they queue behind each other rather than all sleeping
    /// in parallel and then firing at the same moment.
    /// </summary>
    private static readonly SemaphoreSlim _requestGate = new(1, 1);

    /// <summary>
    /// Guards the allowance IMVDb has reported: the limit, what is left of it and when the
    /// window it belongs to is believed to have started. Responses are read outside the
    /// request gate, so several can land at once.
    /// </summary>
    private static readonly object _reportedStateLock = new();

    /// <summary>
    /// When the next request may be sent, on the monotonic clock. Guarded by
    /// <see cref="_requestGate"/> for writes made while holding a slot; a rate limit
    /// response pushes it back from outside the gate through <see cref="DelayNextRequest"/>.
    /// </summary>
    private static long _nextRequestTimestamp = Stopwatch.GetTimestamp();

    /// <summary>
    /// The allowance IMVDb last reported through X-RateLimit-Limit, or 0 before any
    /// response has carried one.
    /// </summary>
    private static int _reportedRequestsPerMinute;

    /// <summary>
    /// What IMVDb last reported to be left of the allowance through X-RateLimit-Remaining,
    /// or -1 before any response has carried one.
    /// </summary>
    private static int _reportedRemaining = -1;

    /// <summary>
    /// When the window <see cref="_reportedRemaining"/> belongs to is believed to have
    /// started, on the monotonic clock. The remaining counts are not dated, so this is taken
    /// from the point the remaining count was last seen to go up, which can only happen when
    /// a window rolls over.
    /// </summary>
    private static long _windowStartTimestamp = Stopwatch.GetTimestamp();

    /// <summary>
    /// Waits until the rate limit allows another request to be sent.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once a request may be sent.</returns>
    internal static async Task WaitForRequestSlot(ILogger logger, CancellationToken cancellationToken)
    {
        // The gate is held across the wait so that concurrent callers queue up.
        await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var waited = false;
            for (var remaining = GetRemainingInterval(); remaining > TimeSpan.Zero; remaining = GetRemainingInterval())
            {
                if (!waited)
                {
                    logger.LogDebug("Waiting {Delay} ms for the IMVDb rate limit.", remaining.TotalMilliseconds);
                    waited = true;
                }

                await Task.Delay(remaining < _minimumDelay ? _minimumDelay : remaining, cancellationToken).ConfigureAwait(false);
            }

            _nextRequestTimestamp = Stopwatch.GetTimestamp() + GetRequestIntervalTicks();
        }
        finally
        {
            _requestGate.Release();
        }
    }

    /// <summary>
    /// Takes note of the allowance IMVDb reported on a successful response, so that the
    /// spacing follows what is actually left rather than the configured allowance.
    /// </summary>
    /// <param name="response">The response to read the X-RateLimit-* headers from.</param>
    /// <param name="logger">The logger.</param>
    internal static void RegisterResponse(HttpResponseMessage response, ILogger logger)
    {
        RegisterReportedLimit(response, logger);

        var remaining = ReadIntHeader(response, "X-RateLimit-Remaining");
        if (remaining < 0)
        {
            // Nothing reported, so the configured spacing is all there is to go on.
            return;
        }

        RegisterReportedRemaining(remaining);

        if (remaining > 0)
        {
            return;
        }

        // The allowance is spent; nothing may be sent until the window resets, whatever the
        // configured spacing says.
        var untilReset = GetTimeUntilReset(response);
        if (untilReset > TimeSpan.Zero)
        {
            logger.LogInformation("IMVDb rate limit exhausted. Holding requests for {Delay} ms.", untilReset.TotalMilliseconds);
            DelayNextRequest(untilReset);
        }
    }

    /// <summary>
    /// Holds every caller back after IMVDb answered with HTTP 429.
    /// </summary>
    /// <param name="response">The rate limited response.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>How long the caller has to wait before retrying.</returns>
    internal static TimeSpan RegisterRateLimited(HttpResponseMessage response, ILogger logger)
    {
        RegisterReportedLimit(response, logger);

        // Whatever the headers say, an HTTP 429 means the allowance is gone.
        RegisterReportedRemaining(0);

        var delay = response.Headers.RetryAfter?.Delta;
        if (delay is null && response.Headers.RetryAfter?.Date is { } retryAfterDate)
        {
            delay = retryAfterDate - DateTimeOffset.UtcNow;
        }

        if (delay is null || delay <= TimeSpan.Zero)
        {
            var reset = ReadIntHeader(response, "X-RateLimit-Reset");
            if (reset > 0)
            {
                delay = DateTimeOffset.FromUnixTimeSeconds(reset) - DateTimeOffset.UtcNow;
            }
        }

        var retryDelay = delay is { } value && value > TimeSpan.Zero ? value : _fallbackRetryDelay;

        // Everything queued behind this caller has to wait it out too, otherwise they
        // each spend a request rediscovering the same 429.
        DelayNextRequest(retryDelay);

        return retryDelay;
    }

    /// <summary>
    /// Takes note of the allowance reported through X-RateLimit-Limit.
    /// </summary>
    private static void RegisterReportedLimit(HttpResponseMessage response, ILogger logger)
    {
        var limit = ReadIntHeader(response, "X-RateLimit-Limit");
        if (limit <= 0)
        {
            return;
        }

        lock (_reportedStateLock)
        {
            if (limit == _reportedRequestsPerMinute)
            {
                return;
            }

            _reportedRequestsPerMinute = limit;
        }

        logger.LogDebug("IMVDb reports a rate limit of {Limit} requests per minute.", limit);
    }

    /// <summary>
    /// Takes note of what IMVDb reported to be left of the allowance, and of the window
    /// rolling over when the count goes back up.
    /// </summary>
    private static void RegisterReportedRemaining(int remaining)
    {
        lock (_reportedStateLock)
        {
            // A count can only go up when the allowance was replenished, so that dates the
            // start of the window it belongs to. The assumed window running out re-dates it
            // too, in case a reset went unnoticed - the counts only arrive one request at a
            // time, and a request that failed outright carries none at all.
            if (remaining > _reportedRemaining || Stopwatch.GetElapsedTime(_windowStartTimestamp) >= _rateLimitWindow)
            {
                _windowStartTimestamp = Stopwatch.GetTimestamp();
            }

            _reportedRemaining = remaining;
        }
    }

    /// <summary>
    /// How long until the allowance is expected to be replenished.
    /// </summary>
    private static TimeSpan GetTimeUntilReset(HttpResponseMessage response)
    {
        var reset = ReadIntHeader(response, "X-RateLimit-Reset");
        if (reset > 0)
        {
            var untilReset = DateTimeOffset.FromUnixTimeSeconds(reset) - DateTimeOffset.UtcNow;
            if (untilReset > TimeSpan.Zero)
            {
                return untilReset;
            }
        }

        // Only rate limited responses tend to carry a reset timestamp, so otherwise fall back
        // to the end of the window the remaining counts have been tracked against.
        var untilWindowEnd = GetTimeUntilWindowEnd();

        return untilWindowEnd > TimeSpan.Zero ? untilWindowEnd : _fallbackRetryDelay;
    }

    /// <summary>
    /// Pushes the next request slot at least <paramref name="delay"/> into the future.
    /// </summary>
    private static void DelayNextRequest(TimeSpan delay)
    {
        var target = Stopwatch.GetTimestamp() + (long)(Stopwatch.Frequency * delay.TotalSeconds);

        // Never pull the slot forward: another response may have asked for longer.
        var current = Interlocked.Read(ref _nextRequestTimestamp);
        while (target > current)
        {
            var previous = Interlocked.CompareExchange(ref _nextRequestTimestamp, target, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }

    private static TimeSpan GetRemainingInterval()
        => Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), Interlocked.Read(ref _nextRequestTimestamp));

    private static TimeSpan GetTimeUntilWindowEnd()
    {
        lock (_reportedStateLock)
        {
            return _rateLimitWindow - Stopwatch.GetElapsedTime(_windowStartTimestamp);
        }
    }

    /// <summary>
    /// The spacing to leave before the next request, in monotonic clock ticks: never less than
    /// the configured allowance asks for, and stretched further when what IMVDb reports to be
    /// left of its allowance is running out faster than that.
    /// </summary>
    private static long GetRequestIntervalTicks()
    {
        var configuredInterval = GetConfiguredIntervalTicks();

        int remaining;
        TimeSpan untilWindowEnd;
        lock (_reportedStateLock)
        {
            remaining = _reportedRemaining;
            untilWindowEnd = _rateLimitWindow - Stopwatch.GetElapsedTime(_windowStartTimestamp);
        }

        if (remaining <= 0 || untilWindowEnd <= TimeSpan.Zero)
        {
            return configuredInterval;
        }

        var spreadInterval = (long)(Stopwatch.Frequency * (untilWindowEnd.TotalSeconds / remaining));

        return Math.Max(configuredInterval, spreadInterval);
    }

    /// <summary>
    /// The spacing between requests the allowance asks for, in monotonic clock ticks. The
    /// configured allowance is capped by the documented ceiling and by whatever IMVDb last
    /// reported, and a configured allowance of zero or less falls back to the default rather
    /// than letting a scan run unthrottled.
    /// </summary>
    private static long GetConfiguredIntervalTicks()
    {
        var configured = ImvdbPlugin.Instance?.Configuration.RateLimit ?? DefaultRequestsPerMinute;
        if (configured <= 0)
        {
            configured = DefaultRequestsPerMinute;
        }

        var requestsPerMinute = Math.Min(configured, MaxRequestsPerMinute);

        int reported;
        lock (_reportedStateLock)
        {
            reported = _reportedRequestsPerMinute;
        }

        if (reported > 0)
        {
            requestsPerMinute = Math.Min(requestsPerMinute, reported);
        }

        if (requestsPerMinute <= 0)
        {
            return 0;
        }

        return (long)(Stopwatch.Frequency * (60d / requestsPerMinute));
    }

    private static int ReadIntHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var values))
        {
            return -1;
        }

        var value = values.FirstOrDefault();

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : -1;
    }
}
