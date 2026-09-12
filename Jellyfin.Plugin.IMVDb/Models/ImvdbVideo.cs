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
    }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the production status code: <c>r</c> released, <c>p</c> in production,
    /// <c>i</c> incomplete or <c>n</c> not released.
    /// </summary>
    [JsonPropertyName("production_status")]
    public string? ProductionStatus { get; set; }

    /// <summary>
    /// Gets or sets the song title.
    /// </summary>
    [JsonPropertyName("song_title")]
    public string? SongTitle { get; set; }

    /// <summary>
    /// Gets or sets the song slug.
    /// </summary>
    [JsonPropertyName("song_slug")]
    public string? SongSlug { get; set; }

    /// <summary>
    /// Gets or sets the IMVDb url.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether alternate versions of the video exist.
    /// </summary>
    [JsonPropertyName("multiple_versions")]
    public bool MultipleVersions { get; set; }

    /// <summary>
    /// Gets or sets the name of this version, such as <c>US Version</c>.
    /// </summary>
    [JsonPropertyName("version_name")]
    public string? VersionName { get; set; }

    /// <summary>
    /// Gets or sets the number of this version.
    /// </summary>
    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is an IMVDb staff pick.
    /// </summary>
    [JsonPropertyName("is_imvdb_pick")]
    public bool IsImvdbPick { get; set; }

    /// <summary>
    /// Gets or sets the aspect ratio.
    /// </summary>
    [JsonPropertyName("aspect_ratio")]
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the credits have been verified.
    /// </summary>
    [JsonPropertyName("verified_credits")]
    public bool VerifiedCredits { get; set; }

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
    /// Gets or sets the credits.
    /// </summary>
    /// <remarks>
    /// Only populated when the video was requested with <c>include=credits</c>.
    /// </remarks>
    [JsonPropertyName("credits")]
    public ImvdbCredits? Credits { get; set; }
}
