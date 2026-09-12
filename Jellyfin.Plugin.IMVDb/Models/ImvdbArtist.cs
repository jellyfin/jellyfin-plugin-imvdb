using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// The IMVDb entity dto.
/// </summary>
public class ImvdbArtist
{
    /// <summary>
    /// Gets or sets the entity id.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity slug.
    /// </summary>
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets the entity url.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the discogs id.
    /// </summary>
    [JsonPropertyName("discogs_id")]
    public int? DiscogsId { get; set; }

    /// <summary>
    /// Gets or sets the byline, such as <c>Director</c>.
    /// </summary>
    [JsonPropertyName("byline")]
    public string? Byline { get; set; }

    /// <summary>
    /// Gets or sets the biography.
    /// </summary>
    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the entity image.
    /// </summary>
    [JsonPropertyName("image")]
    [JsonConverter(typeof(JsonImageResponseConverter))]
    public ImvdbImage? Image { get; set; }

    /// <summary>
    /// Gets or sets the number of videos the entity is the primary artist on.
    /// </summary>
    [JsonPropertyName("artist_video_count")]
    public int ArtistVideoCount { get; set; }

    /// <summary>
    /// Gets or sets the number of videos the entity is a featured artist on.
    /// </summary>
    [JsonPropertyName("featured_video_count")]
    public int FeaturedVideoCount { get; set; }
}
