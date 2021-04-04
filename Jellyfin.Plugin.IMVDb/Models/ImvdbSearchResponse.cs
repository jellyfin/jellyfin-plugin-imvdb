using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models
{
    /// <summary>
    /// IMVDb search response.
    /// </summary>
    public class ImvdbSearchResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImvdbSearchResponse"/> class.
        /// </summary>
        public ImvdbSearchResponse()
        {
            Results = Array.Empty<ImvdbVideo>();
        }

        /// <summary>
        /// Gets or sets the list of results.
        /// </summary>
        [JsonPropertyName("results")]
        public IReadOnlyList<ImvdbVideo> Results { get; set; }
    }
}