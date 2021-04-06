using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models
{
    /// <summary>
    /// IMVDb director.
    /// </summary>
    public class ImvdbDirector
    {
        /// <summary>
        /// Gets or sets the director name.
        /// </summary>
        [JsonPropertyName("entity_name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        [JsonPropertyName("entity_id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        [JsonPropertyName("position_name")]
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        [JsonPropertyName("entity_url")]
        public string Url { get; set; } = string.Empty;
    }
}