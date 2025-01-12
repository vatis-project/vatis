// <copyright file="CloudTypeConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;

namespace Vatsim.Vatis.Profiles.Converter;

/// <summary>
/// Converts JSON data to and from a dictionary of cloud types.
/// </summary>
public class CloudTypeConverter : JsonConverter<Dictionary<string, CloudType>>
{
    /// <summary>
    /// Reads and converts the JSON to a dictionary of cloud types.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A dictionary of cloud types.</returns>
    public override Dictionary<string, CloudType> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Deserialize the JSON into a JsonDocument for inspection
        using var document = JsonDocument.ParseValue(ref reader);
        var rootElement = document.RootElement;

        // Check if we have an object at the root (it should be the "types" object)
        if (rootElement.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, CloudType>();

            // List of required cloud types to ensure they are always present
            var requiredKeys = new List<string>
            {
                "FEW", "SCT", "BKN", "OVC", "VV", "NSC", "NCD", "CLR", "SKC",
            };

            // Iterate through each property in the "types" object
            foreach (var property in rootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // If the value is an object, it's the new format (CloudType)
                    var cloudType = JsonSerializer.Deserialize(
                        property.Value.GetRawText(),
                        SourceGenerationContext.NewDefault.CloudType);
                    if (cloudType != null)
                    {
                        result.Add(property.Name, cloudType);
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    // If the value is a string, it's the legacy format
                    var cloudType = property.Name switch
                    {
                        "FEW" => new CloudType("FEW{altitude}", property.Value.GetString() ?? string.Empty),
                        "SCT" => new CloudType("SCT{altitude}{convective}", property.Value.GetString() ?? string.Empty),
                        "BKN" => new CloudType("BKN{altitude}{convective}", property.Value.GetString() ?? string.Empty),
                        "OVC" => new CloudType("OVC{altitude}{convective}", property.Value.GetString() ?? string.Empty),
                        "VV" => new CloudType("VV{altitude}", property.Value.GetString() ?? string.Empty),
                        "NSC" => new CloudType("NSC", property.Value.GetString() ?? string.Empty),
                        "NCD" => new CloudType("NCD", property.Value.GetString() ?? string.Empty),
                        "CLR" => new CloudType("CLR", property.Value.GetString() ?? string.Empty),
                        "SKC" => new CloudType("SKC", property.Value.GetString() ?? string.Empty),
                        _ => throw new ArgumentException($"Unknown cloud type: {property.Name}"),
                    };
                    result.Add(property.Name, cloudType);
                }
                else
                {
                    // If we encounter an unexpected format, throw an error
                    throw new JsonException($"Unexpected value for cloud type: {property.Name}");
                }
            }

            // Ensure that all required keys are present in the result
            foreach (var key in requiredKeys)
            {
                if (!result.ContainsKey(key))
                {
                    // Add missing keys with default values
                    result.Add(key, CreateDefaultCloudType(key));
                }
            }

            return result;
        }

        throw new JsonException("Invalid JSON format.");
    }

    /// <summary>
    /// Writes the dictionary of cloud types to JSON.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<string, CloudType> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, SourceGenerationContext.NewDefault.DictionaryStringCloudType);
    }

    /// <summary>
    /// Creates a default cloud type for the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>A default cloud type.</returns>
    private CloudType CreateDefaultCloudType(string key)
    {
        return key switch
        {
            "FEW" => new CloudType("FEW{altitude}", "few clouds at {altitude}"),
            "SCT" => new CloudType("SCT{altitude}{convective}", "{altitude} scattered {convective}"),
            "BKN" => new CloudType("BKN{altitude}{convective}", "{altitude} broken {convective}"),
            "OVC" => new CloudType("OVC{altitude}{convective}", "{altitude} overcast {convective}"),
            "VV" => new CloudType("VV{altitude}", "indefinite ceiling {altitude}"),
            "NSC" => new CloudType("NSC", "no significant clouds"),
            "NCD" => new CloudType("NCD", "no clouds detected"),
            "CLR" => new CloudType("CLR", "sky clear below one-two thousand"),
            "SKC" => new CloudType("SKC", "sky clear"),
            _ => throw new ArgumentException($"Unknown cloud type: {key}"),
        };
    }
}
