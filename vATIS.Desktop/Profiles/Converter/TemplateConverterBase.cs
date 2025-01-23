// <copyright file="TemplateConverterBase.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.AtisFormat;

namespace Vatsim.Vatis.Profiles.Converter;

/// <summary>
/// Converts legacy template to the new format.
/// </summary>
public abstract class TemplateConverterBase : JsonConverter<Template>
{
    /// <summary>
    /// Gets the default text value.
    /// </summary>
    protected abstract string DefaultText { get; }

    /// <summary>
    /// Gets the default voice value.
    /// </summary>
    protected abstract string DefaultVoice { get; }

    /// <inheritdoc />
    public override Template? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
            return JsonSerializer.Deserialize(ref reader, SourceGenerationContext.NewDefault.Template);

        if (reader.TokenType == JsonTokenType.String)
        {
            var legacyValue = reader.GetString();
            return !string.IsNullOrEmpty(legacyValue)
                ? new Template { Text = DefaultText, Voice = legacyValue }
                : new Template { Text = DefaultText, Voice = DefaultVoice };
        }

        throw new JsonException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Template value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, SourceGenerationContext.NewDefault.Template);
    }
}
