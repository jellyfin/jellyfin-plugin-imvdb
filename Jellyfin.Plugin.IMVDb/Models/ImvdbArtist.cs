using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models
{
    /// <summary>
    /// The IMVDb Artist dto.
    /// </summary>
    public class ImvdbArtist
    {
        /// <summary>
        /// Gets or sets the artist name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the artist slug.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Gets or sets the artist url.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the discogs id.
        /// </summary>
        [JsonPropertyName("discogs_id")]
        public int DiscogsId { get; set; }
    }
}