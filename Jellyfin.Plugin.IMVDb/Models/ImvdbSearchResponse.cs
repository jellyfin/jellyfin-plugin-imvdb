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
    /// Gets or sets the list of results.
    /// </summary>
    [JsonPropertyName("results")]
    public IReadOnlyList<T> Results { get; set; }
}
