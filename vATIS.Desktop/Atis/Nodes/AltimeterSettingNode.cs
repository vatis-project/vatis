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
public class AltimeterSettingNode : BaseNodeMetarRepository<Value>
{
    private int mPressureHpa;
    private double mPressureInHg;
    private IMetarRepository? mMetarRepository;
    private readonly Dictionary<string, Value> mAltimeterSettings = new();
    
    public override async Task Parse(DecodedMetar metar, IMetarRepository metarRepository)
    {
        mMetarRepository = metarRepository;
        await Parse(metar.Pressure);
    }
    
    public override void Parse(DecodedMetar metar)
    {
        throw new NotImplementedException();
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
                mPressureInHg = pressure.Value.ActualValue / 100.0;
                mPressureHpa = (int)Math.Floor((pressure.Value.ActualValue / 100.0) * 33.86);
            }
            else
            {
                if (pressure.Value != null)
                {
                    mPressureHpa = (int)pressure.Value.ActualValue;
                    mPressureInHg = (int)Math.Floor((pressure.Value.ActualValue * 0.02953) * 100) / 100.0;
                }
            }

            // get custom altimeters
            var matches = Regex.Matches(Station.AtisFormat.Altimeter.Template.Voice!, @"{altimeter\|(\w{4})}",
                RegexOptions.IgnoreCase);
            var tasks = new List<Task>();

            foreach (Match match in matches)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var icao = match.Groups[1].Value;
                    var metar = await mMetarRepository?.GetMetar(icao, triggerMessageBus: false)!;
                    if (metar?.Pressure?.Value != null) mAltimeterSettings[metar.Icao] = metar.Pressure.Value;
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

    public override string ParseTextVariables(Value node, string? format)
    {
        if (format == null)
            return "";

        format = Regex.Replace(format, "{altimeter}", node.ActualValue.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|inhg}", mPressureInHg.ToString("00.00"), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|hpa}", mPressureHpa.ToString(), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|text}", node.ActualValue.ToString("0000").ToSerialFormat()?.ToUpperInvariant() ?? string.Empty, RegexOptions.IgnoreCase);

        var qfeMatch = Regex.Match(format, @"\{qfe\|(\d+)\}", RegexOptions.IgnoreCase);
        if (qfeMatch.Success)
        {
            int.TryParse(qfeMatch.Groups[1].Value, out var elevation);
            var qfe = CalculateQfe(mPressureHpa, elevation);
            format = Regex.Replace(format, @"\{qfe\|(\d+)\}", qfe.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
        }
        
        var secondaryAltimeterMatch = Regex.Match(format, @"{altimeter\|(\w{4})}", RegexOptions.IgnoreCase);
        if (secondaryAltimeterMatch.Success)
        {
            if (mAltimeterSettings.TryGetValue(secondaryAltimeterMatch.Groups[1].Value.ToUpperInvariant(), out var pressure))
            {
                format = Regex.Replace(format, @"{altimeter\|(\w{4})}", pressure.ActualValue.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }
        }

        return format;
    }

    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";
        
        format = Regex.Replace(format, "{altimeter}", ((int)node.ActualValue).ToSerialFormat(), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|inhg}", mPressureInHg.ToString("00.00").ToSerialFormat(Station.AtisFormat.Altimeter.PronounceDecimal) ?? string.Empty, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{altimeter\|hpa}", mPressureHpa.ToSerialFormat(), RegexOptions.IgnoreCase);

        var qfeMatch = Regex.Match(format, @"\{qfe\|(\d+)\}", RegexOptions.IgnoreCase);
        if (qfeMatch.Success)
        {
            int.TryParse(qfeMatch.Groups[1].Value, out var elevation);
            var qfe = CalculateQfe(mPressureHpa, elevation);
            format = Regex.Replace(format, @"\{qfe\|(\d+)\}", qfe.ToSerialFormat(), RegexOptions.IgnoreCase);
        }
        
        var secondaryAltimeterMatch = Regex.Match(format, @"{altimeter\|(\w{4})}", RegexOptions.IgnoreCase);
        if (secondaryAltimeterMatch.Success)
        {
            if (mAltimeterSettings.TryGetValue(secondaryAltimeterMatch.Groups[1].Value.ToUpperInvariant(), out var pressure))
            {
                format = Regex.Replace(format, @"{altimeter\|(\w{4})}", pressure.ActualValue.ToSerialFormat(), RegexOptions.IgnoreCase);
            }
        }
        
        return format;
    }

    private static int CalculateQfe(double qnh, double elevationFeet)
    {
        // Pressure lapse rate: approximately 1 hPa per 30 feet
        const double pressureLapseRateFeet = 30.0;
        
        // Calculate the QFE
        var qfe = qnh - (elevationFeet / pressureLapseRateFeet);
        return (int)qfe;
    }
}