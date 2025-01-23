// <copyright file="PresentWeather.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Converter;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the present weather component of the ATIS format.
/// </summary>
public class PresentWeather : BaseFormat
{
    private static readonly Dictionary<string, string> s_defaultWeatherDescriptors = new()
    {
        // types
        { "DZ", "drizzle" },
        { "RA", "rain" },
        { "SN", "snow" },
        { "SG", "snow grains" },
        { "IC", "ice crystals" },
        { "PL", "ice pellets" },
        { "GR", "hail" },
        { "GS", "small hail" },
        { "UP", "unknown precipitation" },
        { "BR", "mist" },
        { "FG", "fog" },
        { "FU", "smoke" },
        { "VA", "volcanic ash" },
        { "DU", "widespread dust" },
        { "SA", "sand" },
        { "HZ", "haze" },
        { "PY", "spray" },
        { "PO", "well developed dust, sand whirls" },
        { "SQ", "squalls" },
        { "FC", "funnel cloud tornado waterspout" },
        { "SS", "sandstorm" },
        { "DS", "dust storm" },

        // descriptors
        { "PR", "partial" },
        { "BC", "patches" },
        { "MI", "shallow" },
        { "DR", "low drifting" },
        { "BL", "blowing" },
        { "SH", "showers" },
        { "TS", "thunderstorm" },
        { "FZ", "freezing" },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PresentWeather"/> class.
    /// </summary>
    public PresentWeather()
    {
        EnsureDefaultWeatherTypes();

        Template = new Template { Text = "{weather}", Voice = "{weather}", };
    }

    /// <summary>
    /// Gets or sets the light intensity descriptor.
    /// </summary>
    [JsonConverter(typeof(LightIntensityDescriptorConverter))]
    public Template LightIntensity { get; set; } = new() { Text = "-", Voice = "light" };

    /// <summary>
    /// Gets or sets the moderate intensity descriptor.
    /// </summary>
    [JsonConverter(typeof(ModerateIntensityDescriptorConverter))]
    public Template ModerateIntensity { get; set; } = new();

    /// <summary>
    /// Gets or sets the heavy intensity descriptor.
    /// </summary>
    [JsonConverter(typeof(HeavyIntensityDescriptorConverter))]
    public Template HeavyIntensity { get; set; } = new() { Text = "+", Voice = "heavy" };

    /// <summary>
    /// Gets or sets the vicinity descriptor.
    /// </summary>
    [JsonConverter(typeof(VicinityDescriptorConverter))]
    public Template Vicinity { get; set; } = new() { Text = "VC", Voice = "in vicinity" };

    /// <summary>
    /// Gets or sets the dictionary of present weather types.
    /// </summary>
    public Dictionary<string, WeatherDescriptorType> PresentWeatherTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the legacy weather types. This property is obsolete and should not be used.
    /// </summary>
    [Obsolete("Use 'PresentWeatherTypes' instead")]
    [JsonPropertyName("WeatherTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? LegacyWeatherTypes
    {
        get => null;
        set
        {
            if (value != null)
            {
                foreach (var kvp in value)
                {
                    PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the legacy weather descriptors. This property is obsolete and should not be used.
    /// </summary>
    [Obsolete("Use 'PresentWeatherTypes' instead")]
    [JsonPropertyName("weatherDescriptors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? LegacyWeatherDescriptors
    {
        get => null;
        set
        {
            if (value != null)
            {
                foreach (var kvp in value)
                {
                    PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="PresentWeather"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="PresentWeather"/> instance that is a copy of this instance.</returns>
    public PresentWeather Clone()
    {
        return (PresentWeather)MemberwiseClone();
    }

    /// <summary>
    /// Ensures that the default weather types are present in the dictionary.
    /// </summary>
    private void EnsureDefaultWeatherTypes()
    {
        foreach (var kvp in s_defaultWeatherDescriptors)
        {
            if (!PresentWeatherTypes.ContainsKey(kvp.Key))
            {
                PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Represents a weather descriptor type with text and spoken components.
    /// </summary>
    public record WeatherDescriptorType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherDescriptorType"/> class.
        /// </summary>
        /// <param name="text">The text component of the weather descriptor.</param>
        /// <param name="spoken">The spoken component of the weather descriptor.</param>
        public WeatherDescriptorType(string text, string spoken)
        {
            Text = text;
            Spoken = spoken;
        }

        /// <summary>
        /// Gets or sets the text component of the weather descriptor.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the spoken component of the weather descriptor.
        /// </summary>
        public string Spoken { get; set; }
    }
}
