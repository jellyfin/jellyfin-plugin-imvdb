using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.IMVDb.Models;

/// <summary>
/// The IMVDb credits dto, returned for a video when <c>include=credits</c> is requested.
/// </summary>
public class ImvdbCredits
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImvdbCredits"/> class.
    /// </summary>
    public ImvdbCredits()
    {
        Crew = Array.Empty<ImvdbCrewCredit>();
        Cast = Array.Empty<ImvdbCastCredit>();
    }

    /// <summary>
    /// Gets or sets the total number of credits.
    /// </summary>
    [JsonPropertyName("total_credits")]
    public int TotalCredits { get; set; }

    /// <summary>
    /// Gets or sets the crew credits.
    /// </summary>
    [JsonPropertyName("crew")]
    public IReadOnlyList<ImvdbCrewCredit> Crew { get; set; }

    /// <summary>
    /// Gets or sets the cast credits.
    /// </summary>
    [JsonPropertyName("cast")]
    public IReadOnlyList<ImvdbCastCredit> Cast { get; set; }
}
