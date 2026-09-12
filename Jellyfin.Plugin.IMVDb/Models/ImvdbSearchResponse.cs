using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// The IMVDb search response.
/// </summary>
/// <typeparam name="T">The type of response object.</typeparam>
public class ImvdbSearchResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbSearchResponse{T}"/> class.
    /// </summary>
    public ImvdbSearchResponse()
    {
        Results = Array.Empty<T>();
    }

    /// <summary>
    /// Gets or sets the total number of matches reported by the video search.
    /// </summary>
    /// <remarks>
    /// The two search endpoints name this differently; read <see cref="TotalResultCount"/>
    /// instead of picking between this and <see cref="TotalResults"/>.
    /// </remarks>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the total number of matches reported by the entity search.
    /// </summary>
    /// <remarks>
    /// See <see cref="Total"/>.
    /// </remarks>
    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    /// <summary>
    /// Gets or sets the page these results are from, counting from one.
    /// </summary>
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the number of results per page.
    /// </summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the list of results.
    /// </summary>
    [JsonPropertyName("results")]
    public IReadOnlyList<T> Results { get; set; }

    /// <summary>
    /// Gets the total number of matches, whichever of the two names the endpoint reported it
    /// under.
    /// </summary>
    [JsonIgnore]
    public int TotalResultCount
        => Total > 0 ? Total : TotalResults;

    /// <summary>
    /// Gets a value indicating whether another page of results follows this one.
    /// </summary>
    /// <remarks>
    /// A response that carries neither a page number nor a page count is treated as the last
    /// page, so that a malformed response ends the enumeration instead of driving it on.
    /// </remarks>
    [JsonIgnore]
    public bool HasNextPage
        => CurrentPage > 0 && CurrentPage < TotalPages;
}
