// <copyright file="AltimeterSettingNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the altimeter setting.
/// </summary>
public class AltimeterSettingNode : BaseNodeMetarRepository<Value>
{
    private readonly Dictionary<string, Value> _altimeterSettings = new();
    private IMetarRepository? _metarRepository;
    private int _pressureHpa;
    private double _pressureInHg;

    /// <summary>
    /// Gets the QNH in whole hectopascals from a decoded pressure value.
    /// </summary>
    /// <param name="pressure">The decoded pressure value.</param>
    /// <returns>The QNH in hPa, or null if no pressure is available.</returns>
    public static int? GetQnhHpa(Value? pressure)
    {
        if (pressure == null)
            return null;

        return pressure.ActualUnit == Value.Unit.MercuryInch
            ? (int)Math.Floor((pressure.ActualValue / 100.0) * 33.86)
            : (int)pressure.ActualValue;
    }

    /// <summary>
    /// Calculates the QFE for the given elevation.
    /// </summary>
    /// <param name="qnh">The QNH in hPa.</param>
    /// <param name="elevationFeet">The elevation in feet.</param>
    /// <returns>The QFE in hPa.</returns>
    public static int CalculateQfe(double qnh, double elevationFeet)
    {
        // Pressure lapse rate: approximately 1 hPa per 27.3 feet
        const double pressureLapseRateFeet = 27.3;

        // Calculate the QFE
        var qfe = qnh - (elevationFeet / pressureLapseRateFeet);
        return (int)Math.Ceiling(qfe);
    }

     /// <inheritdoc/>
    public override async Task Parse(DecodedMetar metar, IMetarRepository metarRepository)
    {
        _metarRepository = metarRepository;
        await Parse(metar.Pressure);
    }

    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(Value node, string? format)
    {
        if (format == null)
            return "";

        var altimeter = node.ActualValue.ToString(CultureInfo.InvariantCulture);
        var inHg = _pressureInHg.ToString("00.00", CultureInfo.GetCultureInfo("en-US"));
        var altimeterWords = node.ActualValue.ToString("0000").ToSerialFormat()?.ToUpperInvariant() ?? string.Empty;

        format = Regex.Replace(format, "{altimeter}", _ => Render("altimeter", altimeter, altimeter, false), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|inhg}", _ => Render("inhg", inHg, inHg, false), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|hpa}", _ => Render("hpa", _pressureHpa.ToString(), _pressureHpa.ToString(), false), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|text}", _ => Render("text", altimeterWords, altimeter, false), RegexOptions.IgnoreCase);

        var qfeMatch = Regex.Match(format, @"\{qfe\|(\d+)\}", RegexOptions.IgnoreCase);
        if (qfeMatch.Success)
        {
            int.TryParse(qfeMatch.Groups[1].Value, out var elevation);
            var qfe = CalculateQfe(_pressureHpa, elevation);
            var qfeText = qfe.ToString(CultureInfo.InvariantCulture);
            format = Regex.Replace(format, @"\{qfe\|(\d+)\}", _ => Render("qfe", qfeText, qfeText, false), RegexOptions.IgnoreCase);
        }

        var secondaryAltimeterMatch = Regex.Match(format, @"{altimeter\|(\w{4})}", RegexOptions.IgnoreCase);
        if (secondaryAltimeterMatch.Success)
        {
            if (_altimeterSettings.TryGetValue(secondaryAltimeterMatch.Groups[1].Value.ToUpperInvariant(), out var pressure))
            {
                format = Regex.Replace(format, @"{altimeter\|(\w{4})}", pressure.ActualValue.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }
        }

        return format;
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var altimeter = ((int)node.ActualValue).ToString(CultureInfo.InvariantCulture);
        var inHg = _pressureInHg.ToString("00.00", CultureInfo.GetCultureInfo("en-US"));

        format = Regex.Replace(format, "{altimeter}", _ => Render("altimeter", ((int)node.ActualValue).ToSerialFormat(), altimeter, true), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|text}", _ => Render("text", ((int)node.ActualValue).ToSerialFormat(), altimeter, true), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|inhg}", _ => Render("inhg", inHg.ToSerialFormat(Station.AtisFormat.Altimeter.PronounceDecimal) ?? string.Empty, inHg, true), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|hpa}", _ => Render("hpa", _pressureHpa.ToSerialFormat(), _pressureHpa.ToString(), true), RegexOptions.IgnoreCase);

        var qfeMatch = Regex.Match(format, @"\{qfe\|(\d+)\}", RegexOptions.IgnoreCase);
        if (qfeMatch.Success)
        {
            int.TryParse(qfeMatch.Groups[1].Value, out var elevation);
            var qfe = CalculateQfe(_pressureHpa, elevation);
            var qfeDigits = qfe.ToString(CultureInfo.InvariantCulture);
            format = Regex.Replace(format, @"\{qfe\|(\d+)\}", _ => Render("qfe", qfe.ToSerialFormat(), qfeDigits, true), RegexOptions.IgnoreCase);
        }

        var secondaryAltimeterMatch = Regex.Match(format, @"{altimeter\|(\w{4})}", RegexOptions.IgnoreCase);
        if (secondaryAltimeterMatch.Success)
        {
            if (_altimeterSettings.TryGetValue(secondaryAltimeterMatch.Groups[1].Value.ToUpperInvariant(), out var pressure))
            {
                format = Regex.Replace(format, @"{altimeter\|(\w{4})}", pressure.ActualValue.ToSerialFormat(), RegexOptions.IgnoreCase);
            }
        }

        return format;
    }

    private string Render(string variant, string rendered, string digits, bool voice)
    {
        var altimeter = Station?.AtisFormat.Altimeter;
        var format = altimeter?.VariableFormats.GetValueOrDefault(variant);
        return VariableFormatter.Apply(
            rendered,
            digits,
            voice ? format?.Voice : format?.Text,
            !voice,
            altimeter?.PronounceDecimal ?? false);
    }

    private async Task Parse(Pressure? pressure)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (pressure == null)
            return;

        try
        {
            if (pressure.Value?.ActualUnit == Value.Unit.MercuryInch)
            {
                _pressureInHg = pressure.Value.ActualValue / 100.0;
                _pressureHpa = (int)Math.Floor((pressure.Value.ActualValue / 100.0) * 33.86);
            }
            else
            {
                if (pressure.Value != null)
                {
                    _pressureHpa = (int)pressure.Value.ActualValue;
                    _pressureInHg = (int)Math.Floor((pressure.Value.ActualValue * 0.02953) * 100) / 100.0;
                }
            }

            // get custom altimeters
            var templates = $"{Station.AtisFormat.Altimeter.Template.Voice} {Station.AtisFormat.Altimeter.Template.Text}";
            var matches = Regex.Matches(templates, @"{altimeter\|(\w{4})}", RegexOptions.IgnoreCase);
            var tasks = new List<Task>();

            var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in matches)
            {
                var icao = match.Groups[1].Value;

                // "text" and "inhg" are variable variants, not ICAO identifiers.
                if (icao.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                    icao.Equals("inhg", StringComparison.OrdinalIgnoreCase) ||
                    !requested.Add(icao))
                {
                    continue;
                }

                tasks.Add(Task.Run(async () =>
                {
                    var metar = await _metarRepository?.GetMetar(icao, triggerMessageBus: false)!;
                    if (metar?.Pressure?.Value != null) _altimeterSettings[metar.Icao] = metar.Pressure.Value;
                }));
            }

            await Task.WhenAll(tasks);

            if (pressure.Value != null)
            {
                VoiceAtis = ParseVoiceVariables(pressure.Value, Station?.AtisFormat.Altimeter.Template.Voice);
                TextAtis = ParseTextVariables(pressure.Value, Station?.AtisFormat.Altimeter.Template.Text);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to parse altimeter setting");
        }
    }
}
