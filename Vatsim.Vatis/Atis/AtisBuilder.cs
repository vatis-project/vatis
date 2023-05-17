using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.AudioForVatsim;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Weather.Objects;

namespace Vatsim.Vatis.Atis;

public class AtisBuilder : IAtisBuilder
{
    private readonly INavDataRepository mNavDataRepository;
    private readonly ITextToSpeechRequest mTextToSpeechRequest;
    private readonly IAudioManager mAudioManager;
    private readonly IDownloader mDownloader;

    public AtisBuilder(INavDataRepository airportDatabase, ITextToSpeechRequest textToSpeechRequest, IAudioManager audioManager, IDownloader downloader)
    {
        mNavDataRepository = airportDatabase;
        mTextToSpeechRequest = textToSpeechRequest;
        mAudioManager = audioManager;
        mDownloader = downloader;
    }

    public string BuildTextAtis(Composite composite)
    {
        if (composite == null)
        {
            throw new AtisBuilderException("Composite is null");
        }

        if (composite.CurrentPreset == null)
        {
            throw new AtisBuilderException("CurrentPreset is null");
        }

        if (composite.DecodedMetar == null)
        {
            throw new AtisBuilderException("DecodedMetar is null");
        }

        if (composite.AirportData == null)
        {
            composite.AirportData = mNavDataRepository.GetAirport(composite.Identifier) ?? throw new AtisBuilderException($"{composite.Identifier} not found in airport database.");
        }

        ParseNodesFromMetar(composite, out string atisLetter, out List<AtisVariable> variables);

        var template = composite.CurrentPreset.Template;

        foreach (var variable in variables)
        {
            template = template.Replace($"[{variable.Find}:VOX]", variable.VoiceReplace);
            template = template.Replace($"${variable.Find}:VOX", variable.VoiceReplace);

            template = template.Replace($"[{variable.Find}]", variable.TextReplace);
            template = template.Replace($"${variable.Find}", variable.TextReplace);

            if (variable.Aliases != null)
            {
                foreach (var alias in variable.Aliases)
                {
                    template = template.Replace($"[{alias}:VOX]", variable.VoiceReplace);
                    template = template.Replace($"${alias}:VOX", variable.VoiceReplace);

                    template = template.Replace($"[{alias}]", variable.TextReplace);
                    template = template.Replace($"${alias}", variable.TextReplace);
                }
            }
        }

        template = Regex.Replace(template, @"\s+(?=[.,?!])", ""); // remove extra spaces before punctuation
        template = Regex.Replace(template, @"\s+", " ");
        template = Regex.Replace(template, @"(?<=\*)(-?[\,0-9]+)", "$1");
        template = Regex.Replace(template, @"(?<=\#)(-?[\,0-9]+)", "$1");
        template = Regex.Replace(template, @"(?<=\+)([A-Z]{3})", "$1");
        template = Regex.Replace(template, @"(?<=\+)([A-Z]{4})", "$1");
        template = Regex.Replace(template, @"(?<![\w\d])\^((?:0[1-9]|1[0-9]|2[0-9]|3[0-6])(?:[LRC]?))(?![\w\d])", "$1"); // strip caret from single runway parsing ^18R

        if (!composite.CurrentPreset.HasClosingVariable && composite.AtisFormat.ClosingStatement.AutoIncludeClosingStatement)
        {
            var closingTemplate = composite.AtisFormat.ClosingStatement.Template.Text;
            closingTemplate = Regex.Replace(closingTemplate, @"{letter}", composite.AtisLetter);
            closingTemplate = Regex.Replace(closingTemplate, @"{letter\|word}", atisLetter);
            template += closingTemplate;
        }

        return template;
    }

    public async Task<(string, byte[])> BuildVoiceAtis(Composite composite, CancellationToken cancellationToken, bool sandbox = false)
    {
        if (composite == null)
        {
            throw new AtisBuilderException("Composite is null");
        }

        if (composite.CurrentPreset == null)
        {
            throw new AtisBuilderException("CurrentPreset is null");
        }

        if (composite.DecodedMetar == null)
        {
            throw new AtisBuilderException("DecodedMetar is null");
        }

        composite.AirportData = mNavDataRepository.GetAirport(composite.Identifier) ?? throw new AtisBuilderException($"{composite.Identifier} not found in airport database.");

        ParseNodesFromMetar(composite, out string atisLetter, out List<AtisVariable> variables);

        // build ATIS using external source
        if (composite.CurrentPreset.ExternalGenerator.Enabled)
        {
            var externalAtis = await BuildAtisFromExternalSource(composite, composite.DecodedMetar, variables);

            if (externalAtis == null)
            {
                throw new AtisBuilderException("Failed to create external ATIS");
            }

            composite.TextAtis = externalAtis.ToUpper();

            return (null, null);
        }

        // build standard ATIS
        composite.TextAtis = BuildTextAtis(composite);

        var template = composite.CurrentPreset.Template;

        foreach (var variable in variables)
        {
            template = template.Replace($"[{variable.Find}:VOX]", variable.VoiceReplace);
            template = template.Replace($"${variable.Find}:VOX", variable.VoiceReplace);

            template = template.Replace($"[{variable.Find}]", variable.VoiceReplace);
            template = template.Replace($"${variable.Find}", variable.VoiceReplace);

            if (variable.Aliases != null)
            {
                foreach (var alias in variable.Aliases)
                {
                    template = template.Replace($"[{alias}:VOX]", variable.VoiceReplace);
                    template = template.Replace($"${alias}:VOX", variable.VoiceReplace);

                    template = template.Replace($"[{alias}]", variable.VoiceReplace);
                    template = template.Replace($"${alias}", variable.VoiceReplace);
                }
            }
        }

        if (!composite.CurrentPreset.HasClosingVariable && composite.AtisFormat.ClosingStatement.AutoIncludeClosingStatement)
        {
            var voiceTemplate = composite.AtisFormat.ClosingStatement.Template.Voice;
            voiceTemplate = Regex.Replace(voiceTemplate, @"{letter}", composite.AtisLetter);
            voiceTemplate = Regex.Replace(voiceTemplate, @"{letter\|word}", atisLetter);
            template += voiceTemplate;
        }

        if (composite.AtisVoice.UseTextToSpeech)
        {
            var text = FormatForTextToSpeech(template.ToUpper(), composite);
            text = Regex.Replace(text, @"[!?.]*([!?.])", "$1"); // clean up duplicate punctuation one last time
            text = Regex.Replace(text, "\\s+([.,!\":])", "$1");

            // catches multiple ATIS letter button presses in quick succession
            await Task.Delay(sandbox ? 0 : 5000, cancellationToken);

            var synthesizedAudio = await mTextToSpeechRequest.RequestSynthesizedText(text, cancellationToken);

            if (synthesizedAudio != null && sandbox == false)
            {
                await UpdateIds(composite, cancellationToken);

                await mAudioManager.AddOrUpdateBot(synthesizedAudio, composite.AtisCallsign, composite.Frequency, composite.AirportData.Latitude, composite.AirportData.Longitude);
            }

            return (text, synthesizedAudio);
        }
        else
        {
            if (composite.RecordedMemoryStream != null)
            {
                await UpdateIds(composite, cancellationToken);

                await mAudioManager.AddOrUpdateBot(composite.RecordedMemoryStream.ToArray(), composite.AtisCallsign, composite.Frequency, composite.AirportData.Latitude, composite.AirportData.Longitude);
            }
        }

        return (null, null);
    }

    public async Task UpdateIds(Composite composite, CancellationToken cancellationToken)
    {
        if (Debugger.IsAttached)
            return;

        if (string.IsNullOrEmpty(composite.IDSEndpoint) || composite.CurrentPreset == null)
            return;

        var json = new IdsUpdateRequest
        {
            Facility = composite.Identifier,
            Preset = composite.CurrentPreset.Name,
            AtisLetter = composite.AtisLetter,
            AirportConditions = composite.CurrentPreset.AirportConditions.StripNewLineChars(),
            Notams = composite.CurrentPreset.Notams.StripNewLineChars(),
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
            AtisType = composite.AtisType.ToString().ToLowerInvariant()
        };

        try
        {
            await mDownloader.PostJsonAsync(composite.IDSEndpoint, json, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException ex)
        {
            Log.Error(ex.ToString());
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException("PostIdsUpdate Error: " + ex.Message);
        }
    }

    private async Task<string> BuildAtisFromExternalSource(Composite composite, Metar metar, List<AtisVariable> variables)
    {
        if (composite == null)
        {
            throw new AtisBuilderException("Composite is null");
        }

        if (composite.CurrentPreset == null)
        {
            throw new AtisBuilderException("CurrentPreset is null");
        }

        if (metar == null)
        {
            throw new AtisBuilderException("Metar is null");
        }

        var preset = composite.CurrentPreset;
        var data = preset.ExternalGenerator;

        if (data == null)
        {
            throw new AtisBuilderException("ExternalGenerator is null");
        }

        var url = data.Url;

        if (!string.IsNullOrEmpty(url))
        {
            url = url.Replace("$metar", System.Web.HttpUtility.UrlEncode(metar.RawMetar));
            url = url.Replace("$arrrwy", data.Arrival);
            url = url.Replace("$deprwy", data.Departure);
            url = url.Replace("$app", data.Approaches);
            url = url.Replace("$remarks", data.Remarks);
            url = url.Replace("$atiscode", composite.AtisLetter);

            var aptcond = variables.FirstOrDefault(x => x.Find == "ARPT_COND");
            if (aptcond != null)
            {
                url = url.Replace("$aptcond", aptcond.TextReplace);
            }

            var notams = variables.FirstOrDefault(x => x.Find == "NOTAMS");
            if (notams != null)
            {
                url = url.Replace("$notams", notams.TextReplace);
            }

            try
            {
                var response = await mDownloader.DownloadStringAsync(url);
                response = Regex.Replace(response, @"\[(.*?)\]", " $1 ");
                response = Regex.Replace(response, @"\s+", " ");
                return response;
            }
            catch (Exception ex)
            {
                throw new AtisBuilderException("External ATIS Error: " + ex.Message);
            }
        }

        return null;
    }

    private void ParseNodesFromMetar(Composite composite, out string atisLetter, out List<AtisVariable> variables)
    {
        if (composite.DecodedMetar == null)
        {
            atisLetter = null;
            variables = null;
            return;
        }

        var metar = composite.DecodedMetar;
        var time = NodeParser.Parse<ObservationTimeNode, ObservationDayTime>(metar, composite);
        var surfaceWind = NodeParser.Parse<SurfaceWindNode, SurfaceWind>(metar, composite);
        var rvr = NodeParser.Parse<RunwayVisualRangeNode, RunwayVisualRange>(metar, composite);
        var visibility = NodeParser.Parse<PrevailingVisibilityNode, PrevailingVisibility>(metar, composite);
        var presentWeather = NodeParser.Parse<PresentWeatherNode, WeatherPhenomena>(metar, composite);
        var clouds = NodeParser.Parse<CloudNode, CloudLayer>(metar, composite);
        var temp = NodeParser.Parse<TemperatureNode, TemperatureInfo>(metar, composite);
        var dew = NodeParser.Parse<DewpointNode, TemperatureInfo>(metar, composite);
        var pressure = NodeParser.Parse<AltimeterSettingNode, AltimeterSetting>(metar, composite);
        var trends = NodeParser.Parse<TrendNode, Trend>(metar, composite);

        atisLetter = char.Parse(composite.AtisLetter).ToPhonetic();
        var completeWxStringVoice = $"{surfaceWind.VoiceAtis} {visibility.VoiceAtis} {rvr.VoiceAtis} {presentWeather.VoiceAtis} {clouds.VoiceAtis} {temp.VoiceAtis} {dew.VoiceAtis} {pressure.VoiceAtis}";
        var completeWxStringAcars = $"{surfaceWind.TextAtis} {visibility.TextAtis} {rvr.TextAtis} {presentWeather.TextAtis} {clouds.TextAtis} {temp.TextAtis}{(!string.IsNullOrEmpty(temp.TextAtis) || !string.IsNullOrEmpty(dew.TextAtis) ? "/" : "")}{dew.TextAtis} {pressure.TextAtis}";

        var airportConditions = "";
        if (!string.IsNullOrEmpty(composite.CurrentPreset.AirportConditions) || composite.AirportConditionDefinitions.Any(t => t.Enabled))
        {
            if (composite.AirportConditionsBeforeFreeText)
            {
                airportConditions = string.Join(" ", string.Join(". ", composite.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.AirportConditions);
            }
            else
            {
                airportConditions = string.Join(" ", composite.CurrentPreset.AirportConditions, string.Join(". ", composite.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)));
            }
        }

        airportConditions = Regex.Replace(airportConditions, @"[!?.]*([!?.])", "$1"); // clean up duplicate punctuation
        airportConditions = Regex.Replace(airportConditions, "\\s+([.,!\":])", "$1");

        var notamVoice = "";
        var notamText = "";
        if (!string.IsNullOrEmpty(composite.CurrentPreset.Notams) || composite.NotamDefinitions.Any(t => t.Enabled))
        {
            if (composite.UseNotamPrefix)
            {
                notamVoice = composite.IsFaaAtis ? "Notices to air missions. " : "Notices to airmen. ";
            }

            if (composite.NotamsBeforeFreeText)
            {
                notamText = string.Join(" ", string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.Notams);
                notamVoice += string.Join(" ", string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.Notams);
            }
            else
            {
                notamText = string.Join(". ", composite.CurrentPreset.Notams, string.Join(" ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)));
                notamVoice += string.Join(" ", composite.CurrentPreset.Notams, string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)));
            }
        }

        notamVoice = Regex.Replace(notamVoice, @"[!?.]*([!?.])", "$1"); // clean up duplicate punctuation
        notamVoice = Regex.Replace(notamVoice, "\\s+([.,!\":])", "$1");
        notamText = Regex.Replace(notamText, @"[!?.]*([!?.])", "$1"); // clean up duplicate punctuation
        notamText = Regex.Replace(notamText, "\\s+([.,!\":])", "$1");

        if (!string.IsNullOrEmpty(notamText) && composite.IsFaaAtis)
        {
            notamText = "NOTAMS... " + notamText;
        }

        variables = new List<AtisVariable>
        {
            new AtisVariable("FACILITY", composite.AirportData.ID, composite.AirportData.Name),
            new AtisVariable("ATIS_LETTER", composite.AtisLetter, atisLetter,  new [] {"LETTER","ATIS_CODE","ID"}),
            new AtisVariable("TIME", time.TextAtis, time.VoiceAtis, new []{"OBS_TIME","OBSTIME"}),
            new AtisVariable("WIND", surfaceWind.TextAtis, surfaceWind.VoiceAtis, new[]{"SURFACE_WIND"}),
            new AtisVariable("RVR", rvr.TextAtis, rvr.VoiceAtis),
            new AtisVariable("VIS", visibility.TextAtis, visibility.VoiceAtis, new[]{"PREVAILING_VISIBILITY"}),
            new AtisVariable("PRESENT_WX", presentWeather.TextAtis, presentWeather.VoiceAtis, new[]{"PRESENT_WEATHER"}),
            new AtisVariable("CLOUDS", clouds.TextAtis, clouds.VoiceAtis),
            new AtisVariable("TEMP", temp.TextAtis, temp.VoiceAtis),
            new AtisVariable("DEW", dew.TextAtis, dew.VoiceAtis),
            new AtisVariable("PRESSURE", pressure.TextAtis, pressure.VoiceAtis, new[]{"QNH"}),
            new AtisVariable("WX", completeWxStringAcars, completeWxStringVoice, new[]{"FULL_WX_STRING"}),
            new AtisVariable("ARPT_COND", airportConditions, airportConditions, new[]{"ARRDEP"}),
            new AtisVariable("NOTAMS", notamText, notamVoice),
            new AtisVariable("TREND", trends.TextAtis, trends.VoiceAtis)
        };

        if (composite.AtisFormat.TransitionLevel != null)
        {
            var trl = composite.AtisFormat.TransitionLevel.Values.FirstOrDefault(t =>
            {
                return composite.DecodedMetar.AltimeterSetting.Value >= t.Low
                && composite.DecodedMetar.AltimeterSetting.Value <= t.High;
            });

            if (trl != null)
            {
                var trlTemplateText = composite.AtisFormat.TransitionLevel.Template.Text;
                trlTemplateText = Regex.Replace(trlTemplateText, @"{trl}", trl.Altitude.ToString());
                trlTemplateText = Regex.Replace(trlTemplateText, @"{trl\|text}", trl.Altitude.ToSerialForm());

                var trlTemplateVoice = composite.AtisFormat.TransitionLevel.Template.Voice;
                trlTemplateVoice = Regex.Replace(trlTemplateVoice, @"{trl}", trl.Altitude.ToString());
                trlTemplateVoice = Regex.Replace(trlTemplateVoice, @"{trl\|text}", trl.Altitude.ToSerialForm());

                variables.Add(new AtisVariable("TL", trlTemplateText, trlTemplateVoice));
            }
            else
            {
                variables.Add(new AtisVariable("TL", "", ""));
            }
        }

        var closingTextTemplate = composite.AtisFormat.ClosingStatement.Template.Text;
        var closingVoiceTemplate = composite.AtisFormat.ClosingStatement.Template.Voice;

        if (!string.IsNullOrEmpty(closingTextTemplate) && !string.IsNullOrEmpty(closingVoiceTemplate))
        {
            closingTextTemplate = Regex.Replace(closingTextTemplate, @"{letter}", composite.AtisLetter);
            closingTextTemplate = Regex.Replace(closingTextTemplate, @"{letter\|word}", atisLetter);

            closingVoiceTemplate = Regex.Replace(closingVoiceTemplate, @"{letter}", atisLetter);
            closingVoiceTemplate = Regex.Replace(closingVoiceTemplate, @"{letter\|word}", atisLetter);

            variables.Add(new AtisVariable("CLOSING", closingTextTemplate, closingVoiceTemplate));
        }
        else
        {
            variables.Add(new AtisVariable("CLOSING", "", ""));
        }
    }

    private string FormatForTextToSpeech(string input, Composite composite)
    {
        // user defined contractions
        foreach (var contraction in composite.Contractions)
        {
            input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(contraction.String)}(?![\w\d])", contraction.Spoken, RegexOptions.IgnoreCase);
        }

        // airports and navaid identifiers
        var navdataMatches = Regex.Matches(input, @"\+([A-Z0-9]{3,4})");
        if (navdataMatches.Count > 0)
        {
            foreach (Match match in navdataMatches)
            {
                if (match.Groups == null || match.Groups.Count == 0)
                    continue;

                var navaid = mNavDataRepository.GetNavaid(match.Groups[1].Value);
                if (navaid != null)
                {
                    input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(match.Value)}(?![\w\d])", navaid.Name);
                }
                else
                {
                    var airport = mNavDataRepository.GetAirport(match.Groups[1].Value);
                    if (airport != null)
                    {
                        input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(match.Value)}(?![\w\d])", airport.Name);
                    }
                }
            }
        }

        // parse zulu times
        input = Regex.Replace(input, @"([0-9])([0-9])([0-9])([0-8])Z",
            m => string.Format($"{int.Parse(m.Groups[1].Value).ToSerialForm()} " +
                               $"{int.Parse(m.Groups[2].Value).ToSerialForm()} " +
                               $"{int.Parse(m.Groups[3].Value).ToSerialForm()} " +
                               $"{int.Parse(m.Groups[4].Value).ToSerialForm()} zulu"));

        // vhf frequencies
        input = Regex.Replace(input, @"(1\d\d\.\d\d?\d?)", m => m.Groups[1].Value.ToSerialForm(composite.UseDecimalTerminology));

        // letters
        input = Regex.Replace(input, @"\*([A-Z]{1,2}[0-9]{0,2})", m => m.Value.ToAlphaNumericWordGroup()).Trim();

        // parse taxiways
        input = Regex.Replace(input, @"\bTWY ([A-Z]{1,2}[0-9]{0,2})\b", m => $"TWY {m.Groups[1].Value.ToAlphaNumericWordGroup()}");
        input = Regex.Replace(input, @"\bTWYS ([A-Z]{1,2}[0-9]{0,2})\b", m => $"TWYS {m.Groups[1].Value.ToAlphaNumericWordGroup()}");

        // parse runways
        input = Regex.Replace(input, @"\b(RY|RWY|RWYS|RUNWAY|RUNWAYS)\s?([0-9]{1,2})([LRC]?)\b",
            m => StringExtensions.RwyNumbersToWords(int.Parse(m.Groups[2].Value), m.Groups[3].Value,
                prefix: !string.IsNullOrEmpty(m.Groups[1].Value),
                plural: !string.IsNullOrEmpty(m.Groups[1].Value) &&
                        (m.Groups[1].Value == "RWYS" || m.Groups[1].Value == "RUNWAYS"),
                leadingZero: !composite.IsFaaAtis));

        // parse individual runway: ^18R
        var runwayMatches = Regex.Matches(input, @"\^(0[1-9]|1[0-9]|2[0-9]|3[0-6])([LRC]?)");
        if (runwayMatches.Count > 0)
        {
            foreach (Match rwy in runwayMatches)
            {
                var designator = "";
                switch (rwy.Groups[2].Value)
                {
                    case "L":
                        designator = "left";
                        break;
                    case "R":
                        designator = "right";
                        break;
                    case "C":
                        designator = "center";
                        break;
                }

                var replace = int.Parse(rwy.Groups[1].Value).ToSerialForm(leadingZero: !composite.IsFaaAtis) + " " + designator;
                input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(rwy.Value)}(?![\w\d])", replace.Trim());
            }
        }

        // read numbers in group format, prefixed with # or surrounded with {}
        input = Regex.Replace(input, @"\*(-?[\,0-9]+)", m => int.Parse(m.Groups[1].Value.Replace(",", "")).ToGroupForm());
        input = Regex.Replace(input, @"\{(-?[\,0-9]+)\}", m => int.Parse(m.Groups[1].Value.Replace(",", "")).ToGroupForm());

        // read numbers in serial format
        input = Regex.Replace(input, @"([+-])?([0-9]+\.[0-9]+|[0-9]+|\.[0-9]+)(?![^{]*\})", m => m.Value.ToSerialForm(composite.UseDecimalTerminology));

        input = Regex.Replace(input, @"(?<=\*)(-?[\,0-9]+)", "$1");
        input = Regex.Replace(input, @"(?<=\#)(-?[\,0-9]+)", "$1");
        input = Regex.Replace(input, @"\{(-?[\,0-9]+)\}", "$1");
        input = Regex.Replace(input, @"(?<=\+)([A-Z]{3})", "$1");
        input = Regex.Replace(input, @"(?<=\+)([A-Z]{4})", "$1");
        input = Regex.Replace(input, @"[!?.]*([!?.])", "$1 "); // clean up duplicate punctuation
        input = Regex.Replace(input, "\\s+([.,!\":])", "$1 ");
        input = Regex.Replace(input, @"\s+", " ");
        input = Regex.Replace(input, @"\s\,", ",");
        input = Regex.Replace(input, @"\&", "and");
        input = Regex.Replace(input, @"\*", "");

        return input.ToUpper();
    }
}