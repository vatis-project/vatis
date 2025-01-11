using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes.Converter;

public class ObservationTimeConverter : JsonConverter<List<int>>
{
    public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the token is a single integer
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Read the integer and return it as a list
            var value = reader.GetInt32();
            return [value];
        }

        // Otherwise, deserialize to a List<int>
        return JsonSerializer.Deserialize(ref reader, SourceGenerationContext.NewDefault.ListInt32);
    }

    public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
    {
        // Serialize the list of integers
        JsonSerializer.Serialize(writer, value, SourceGenerationContext.NewDefault.ListInt32);
    }
}
