// <copyright file="ObservationTimeConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes.Converter;

/// <summary>
/// Provides a custom JSON converter for converting observation time data between JSON and a list of integers.
/// </summary>
public class ObservationTimeConverter : JsonConverter<List<int>>
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
    {
        // Serialize the list of integers
        JsonSerializer.Serialize(writer, value, SourceGenerationContext.NewDefault.ListInt32);
    }
}
