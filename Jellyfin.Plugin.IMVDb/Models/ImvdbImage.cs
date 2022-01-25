using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// IMVDb image dto.
/// </summary>
public class ImvdbImage
{
    /// <summary>
    /// Gets or sets the <c>o</c> image url.
    /// </summary>
    /// <remarks>
    /// Raw, largest image.
    /// </remarks>
    [JsonPropertyName("o")]
    public string? Size1 { get; set; }

    /// <summary>
    /// Gets or sets the <c>l</c> image url.
    /// </summary>
    /// <remarks>
    /// 224px by 126px.
    /// </remarks>
    [JsonPropertyName("l")]
    public string? Size2 { get; set; }

    /// <summary>
    /// Gets or sets the <c>b</c> image url.
    /// </summary>
    /// <remarks>
    /// 125px by 70px.
    /// </remarks>
    [JsonPropertyName("b")]
    public string? Size3 { get; set; }

    /// <summary>
    /// Gets or sets the <c>t</c> image url.
    /// </summary>
    /// <remarks>
    /// 50px by 28px.
    /// </remarks>
    [JsonPropertyName("t")]
    public string? Size4 { get; set; }
}
