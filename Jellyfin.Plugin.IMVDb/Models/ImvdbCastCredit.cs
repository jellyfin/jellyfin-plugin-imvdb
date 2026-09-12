using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// An IMVDb cast credit.
/// </summary>
public class ImvdbCastCredit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbCastCredit"/> class.
    /// </summary>
    public ImvdbCastCredit()
    {
        Roles = Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the credited entity's name.
    /// </summary>
    [JsonPropertyName("entity_name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the credited entity's slug.
    /// </summary>
    [JsonPropertyName("entity_slug")]
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets the credited entity's id.
    /// </summary>
    [JsonPropertyName("entity_id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the roles played. Often empty for music videos.
    /// </summary>
    [JsonPropertyName("cast_roles")]
    public IReadOnlyList<string> Roles { get; set; }

    /// <summary>
    /// Gets or sets the position id.
    /// </summary>
    [JsonPropertyName("position_id")]
    public int PositionId { get; set; }
}
