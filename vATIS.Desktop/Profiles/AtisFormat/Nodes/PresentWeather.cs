using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

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
        { "FZ", "freezing" }
    };

    public PresentWeather()
    {
        this.EnsureDefaultWeatherTypes();

        this.Template = new Template
        {
            Text = "{weather}",
            Voice = "{weather}"
        };
    }

    public string LightIntensity { get; set; } = "light";

    public string ModerateIntensity { get; set; } = "";

    public string HeavyIntensity { get; set; } = "heavy";

    public string Vicinity { get; set; } = "in vicinity";

    public Dictionary<string, WeatherDescriptorType> PresentWeatherTypes { get; set; } = new();

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
                    this.PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    // Legacy property for "weatherDescriptors" in JSON
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
                    this.PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
                }
            }
        }
    }

    private void EnsureDefaultWeatherTypes()
    {
        foreach (var kvp in s_defaultWeatherDescriptors)
        {
            if (!this.PresentWeatherTypes.ContainsKey(kvp.Key))
            {
                this.PresentWeatherTypes[kvp.Key] = new WeatherDescriptorType(kvp.Key, kvp.Value);
            }
        }
    }

    public PresentWeather Clone()
    {
        return (PresentWeather)this.MemberwiseClone();
    }

    public record WeatherDescriptorType
    {
        public WeatherDescriptorType(string text, string spoken)
        {
            this.Text = text;
            this.Spoken = spoken;
        }

        public string Text { get; set; }

        public string Spoken { get; set; }
    }
}