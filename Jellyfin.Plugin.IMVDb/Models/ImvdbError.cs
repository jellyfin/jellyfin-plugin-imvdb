using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// The error object IMVDb answers a failed request with.
/// </summary>
public class ImvdbError
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded. Always <c>false</c> on an
    /// error response.
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    /// <summary>
    /// Gets or sets the error message, such as <c>Invalid API Key.</c>.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
