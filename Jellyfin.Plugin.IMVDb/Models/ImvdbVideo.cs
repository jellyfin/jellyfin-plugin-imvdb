using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// Imvdb Video dto.
/// </summary>
public class ImvdbVideo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbVideo"/> class.
    /// </summary>
    public ImvdbVideo()
    {
        Artists = Array.Empty<ImvdbArtist>();
        Directors = Array.Empty<ImvdbDirector>();
    }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the song title.
    /// </summary>
    [JsonPropertyName("song_title")]
    public string? SongTitle { get; set; }

    /// <summary>
    /// Gets or sets the IMVDb url.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the list of artists.
    /// </summary>
    [JsonPropertyName("artists")]
    public IReadOnlyList<ImvdbArtist> Artists { get; set; }

    /// <summary>
    /// Gets or sets the images.
    /// </summary>
    [JsonPropertyName("image")]
    [JsonConverter(typeof(JsonImageResponseConverter))]
    public ImvdbImage? Image { get; set; }

    /// <summary>
    /// Gets or sets the directors.
    /// </summary>
    [JsonPropertyName("directors")]
    public IReadOnlyList<ImvdbDirector> Directors { get; set; }
}
