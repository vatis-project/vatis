using System;
using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class TrendNode : BaseNode<TrendForecast>
{
    public override void Parse(DecodedMetar metar)
    {
        if (metar.TrendForecast == null)
        {
            return;
        }

        var tts = new List<string>();
        var acars = new List<string>();

        tts.Add("TREND");
        switch (metar.TrendForecast.ChangeIndicator)
        {
            case TrendForecastType.Becoming:
                tts.Add("BECOMING");
                acars.Add("BECMG");
                break;
            case TrendForecastType.Temporary:
                tts.Add("TEMPORARY");
                acars.Add("TEMPO");
                break;
            case TrendForecastType.NoSignificantChanges:
                tts.Add("NO SIGNIFICANT CHANGES");
                acars.Add("NOSIG");
                break;
        }

        if (metar.TrendForecast.Forecast != null)
        {
        }

        this.VoiceAtis = string.Join(". ", tts);
        this.TextAtis = string.Join(" ", acars);
    }

    public override string ParseTextVariables(TrendForecast value, string? format)
    {
        throw new NotImplementedException();
    }

    public override string ParseVoiceVariables(TrendForecast node, string? format)
    {
        throw new NotImplementedException();
    }
}