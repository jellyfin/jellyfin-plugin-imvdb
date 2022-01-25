using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// The IMVDb Artist dto.
/// </summary>
public class ImvdbArtist
{
    /// <summary>
    /// Gets or sets the artist id.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist url.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the discogs id.
    /// </summary>
    [JsonPropertyName("discogs_id")]
    public int DiscogsId { get; set; }
}
