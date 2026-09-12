using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.IMVDb.Models;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.IMVDb;

/// <summary>
/// The IMVDb client interface.
/// </summary>
public interface IImvdbClient
{
    /// <summary>
    /// Searches for music videos.
    /// </summary>
    /// <param name="searchInfo">The search info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching videos, across as many pages as are enumerated.</returns>
    public IAsyncEnumerable<ImvdbVideo> GetVideoSearchResultsAsync(
        MusicVideoInfo searchInfo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches for entities.
    /// </summary>
    /// <param name="searchInfo">The search info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching entities, across as many pages as are enumerated.</returns>
    public IAsyncEnumerable<ImvdbArtist> GetArtistSearchResultsAsync(
        ArtistInfo searchInfo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get result by id.
    /// </summary>
    /// <param name="imvdbId">The IMBDb id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The IMVDB video.</returns>
    public Task<ImvdbVideo?> GetVideoIdResultAsync(
        string imvdbId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get result by id.
    /// </summary>
    /// <param name="imvdbId">The IMBDb id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The IMVDB entity.</returns>
    public Task<ImvdbArtist?> GetArtistIdResultAsync(
        string imvdbId,
        CancellationToken cancellationToken);
}
