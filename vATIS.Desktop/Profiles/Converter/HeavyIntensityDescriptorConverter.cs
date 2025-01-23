using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.AtisFormat;

namespace Vatsim.Vatis.Profiles.Converter;

public class HeavyIntensityDescriptorConverter : JsonConverter<Template>
{
    /// <inheritdoc />
    public override Template? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
            return JsonSerializer.Deserialize<Template>(ref reader, options);

        if (reader.TokenType == JsonTokenType.String)
        {
            var legacyValue = reader.GetString();

            return !string.IsNullOrEmpty(legacyValue)
                ? new Template { Text = "+", Voice = legacyValue }
                : new Template { Text = "+", Voice = "heavy" };
        }

        throw new JsonException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Template value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, SourceGenerationContext.NewDefault.Template);
    }
}
