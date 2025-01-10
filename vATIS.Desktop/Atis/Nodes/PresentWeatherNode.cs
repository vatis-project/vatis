using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class PresentWeatherNode : BaseNode<WeatherPhenomenon>
{
    public override void Parse(DecodedMetar metar)
    {
        this.Parse(metar.PresentWeather);
    }

    private void Parse(List<WeatherPhenomenon> weathers)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        var voiceAtis = new List<string>();
        var textAtis = new List<string>();

        foreach (var weather in weathers)
        {
            voiceAtis.Add(this.FormatWeatherVoice(weather));
            textAtis.Add(this.FormatWeatherText(weather));
        }

        var voiceTemplate = this.Station.AtisFormat.PresentWeather.Template.Voice;
        var textTemplate = this.Station.AtisFormat.PresentWeather.Template.Text;

        if (voiceTemplate != null)
        {
            this.VoiceAtis = voiceAtis.Count > 0
                ? Regex.Replace(
                    voiceTemplate,
                    "{weather}",
                    string.Join(", ", voiceAtis).Trim(',').Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }

        if (textTemplate != null)
        {
            this.TextAtis = textAtis.Count > 0
                ? Regex.Replace(
                    textTemplate,
                    "{weather}",
                    string.Join(" ", textAtis).Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }
    }

    private string FormatWeatherVoice(WeatherPhenomenon? weather)
    {
        if (weather == null)
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(this.Station);

        var result = new List<string>();

        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(this.Station.AtisFormat.PresentWeather.LightIntensity);
                break;
            case "+":
                result.Add(this.Station.AtisFormat.PresentWeather.HeavyIntensity);
                break;
            default:
                result.Add(this.Station.AtisFormat.PresentWeather.ModerateIntensity);
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
        {
            result.Add(this.Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Spoken);
        }

        result.AddRange(
            weather.Types.Select(type => this.Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Spoken));

        if (weather.IntensityProximity == "VC")
        {
            result.Add(this.Station.AtisFormat.PresentWeather.Vicinity);
        }

        return string.Join(" ", result);
    }

    private string FormatWeatherText(WeatherPhenomenon? weather)
    {
        if (weather == null)
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(this.Station);

        var result = new List<string>();

        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(this.Station.AtisFormat.PresentWeather.LightIntensity);
                break;
            case "+":
                result.Add(this.Station.AtisFormat.PresentWeather.HeavyIntensity);
                break;
            default:
                result.Add(this.Station.AtisFormat.PresentWeather.ModerateIntensity);
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
        {
            result.Add(this.Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Text);
        }

        result.AddRange(
            weather.Types.Select(type => this.Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Text));

        if (weather.IntensityProximity == "VC")
        {
            result.Add(this.Station.AtisFormat.PresentWeather.Vicinity);
        }

        return string.Join(" ", result);
    }

    public override string ParseTextVariables(WeatherPhenomenon value, string? format)
    {
        throw new NotImplementedException();
    }

    public override string ParseVoiceVariables(WeatherPhenomenon node, string? format)
    {
        throw new NotImplementedException();
    }
}