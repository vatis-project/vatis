// <copyright file="TrendForecast.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public sealed class TrendForecast
{
    public TrendForecastType ChangeIndicator { get; set; } = TrendForecastType.None;
    public string? FromTime { get; set; }
    public string? UntilTime { get; set; }
    public string? AtTime { get; set; }
    public string? Forecast { get; set; }
}

public enum TrendForecastType
{
    None,
    Becoming,
    Temporary,
    NoSignificantChanges
}