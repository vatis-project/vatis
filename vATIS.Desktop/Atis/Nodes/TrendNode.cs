// <copyright file="TrendNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides trend information.
/// </summary>
public class TrendNode : BaseNode<TrendForecast>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        if (metar.TrendForecast == null)
            return;

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
            // TODO: Implement trend forecast parsing
        }

        VoiceAtis = string.Join(". ", tts);
        TextAtis = string.Join(" ", acars);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(TrendForecast value, string? format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(TrendForecast node, string? format)
    {
        throw new NotImplementedException();
    }
}
