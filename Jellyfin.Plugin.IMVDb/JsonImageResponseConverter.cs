using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.IMVDb.Models;

namespace Jellyfin.Plugin.IMVDb;

/// <summary>
///     Json converter to ignore empty array when response should be an object.
/// </summary>
public class JsonImageResponseConverter : JsonConverter<ImvdbImage?>
{
    /// <inheritdoc />
    public override ImvdbImage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // For some reason IMVDb returns an empty array instead of an empty object or null when no results found.
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Read end array.
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException("Found actual data, expected empty array");
            }

            return null;
        }

        return JsonSerializer.Deserialize<ImvdbImage>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ImvdbImage? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
