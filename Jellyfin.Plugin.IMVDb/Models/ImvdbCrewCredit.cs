using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// An IMVDb crew credit.
/// </summary>
public class ImvdbCrewCredit
{
    /// <summary>
    /// The IMVDb position code identifying a director credit.
    /// </summary>
    public const string DirectorPositionCode = "dir";

    /// <summary>
    /// The IMVDb position code identifying a producer credit.
    /// </summary>
    public const string ProducerPositionCode = "prod";

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
    /// Gets or sets the human readable position name, such as <c>Director</c>.
    /// </summary>
    [JsonPropertyName("position_name")]
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position code, such as <c>dir</c>.
    /// </summary>
    [JsonPropertyName("position_code")]
    public string? PositionCode { get; set; }

    /// <summary>
    /// Gets or sets the free form notes attached to the credit.
    /// </summary>
    [JsonPropertyName("position_notes")]
    public string? PositionNotes { get; set; }

    /// <summary>
    /// Gets or sets the position id.
    /// </summary>
    [JsonPropertyName("position_id")]
    public int PositionId { get; set; }
}
