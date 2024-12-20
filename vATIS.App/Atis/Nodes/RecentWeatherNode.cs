using System;
using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class RecentWeatherNode : BaseNode<WeatherPhenomenon>
{
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.RecentWeather);
    }

    private void Parse(WeatherPhenomenon? recentWeather)
    {
        if (recentWeather == null)
            return;

        ArgumentNullException.ThrowIfNull(Station);

        TextAtis = FormatWeatherText(recentWeather);
        VoiceAtis = FormatWeatherVoice(recentWeather);
    }

    private string FormatWeatherVoice(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;

        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();

        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(Station.AtisFormat.PresentWeather.LightIntensity);
                break;
            case "+":
                result.Add(Station.AtisFormat.PresentWeather.HeavyIntensity);
                break;
            default:
                result.Add(Station.AtisFormat.PresentWeather.ModerateIntensity);
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
            result.Add(Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Spoken);

        result.AddRange(
            weather.Types.Select(type => Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Spoken));

        if (weather.IntensityProximity == "VC")
            result.Add(Station.AtisFormat.PresentWeather.Vicinity);

        return result.Count > 0 ? $"RECENT {string.Join(", ", result.ToArray())}" : string.Empty;
    }

    private string FormatWeatherText(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;
        
        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();
        
        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(Station.AtisFormat.PresentWeather.LightIntensity);
                break;
            case "+":
                result.Add(Station.AtisFormat.PresentWeather.HeavyIntensity);
                break;
            default:
                result.Add(Station.AtisFormat.PresentWeather.ModerateIntensity);
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
            result.Add(Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Text);

        result.AddRange(weather.Types.Select(type => Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Text));
        
        if(weather.IntensityProximity == "VC")
            result.Add(Station.AtisFormat.PresentWeather.Vicinity);

        return string.Join(" ", result);
    }

    public override string ParseVoiceVariables(WeatherPhenomenon node, string? format)
    {
        throw new System.NotImplementedException();
    }

    public override string ParseTextVariables(WeatherPhenomenon value, string? format)
    {
        throw new System.NotImplementedException();
    }
}