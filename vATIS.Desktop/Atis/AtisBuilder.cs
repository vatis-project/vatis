using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

public class AtisBuilder : IAtisBuilder
{
    private readonly IDownloader? mDownloader;
    private readonly IMetarRepository? mMetarRepository;
    private readonly INavDataRepository? mNavDataRepository;
    private readonly ITextToSpeechService? mTextToSpeechService;

    public AtisBuilder(IDownloader downloader, INavDataRepository navDataRepository,
        ITextToSpeechService textToSpeechService, IMetarRepository metarRepository)
    {
        mDownloader = downloader;
        mMetarRepository = metarRepository;
        mNavDataRepository = navDataRepository;
        mTextToSpeechService = textToSpeechService;
    }

    public async Task<AtisBuilderVoiceAtisResponse> BuildVoiceAtis(AtisStation station, AtisPreset preset, char currentAtisLetter, DecodedMetar decodedMetar,
        CancellationToken cancellationToken, bool sandboxRequest = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(decodedMetar);
        
        var airportData = mNavDataRepository?.GetAirport(station.Identifier) ??
                          throw new AtisBuilderException($"{station.Identifier} not found in airport database.");

        var variables = await ParseNodesFromMetar(station, preset, decodedMetar, airportData, currentAtisLetter);

        var (spokenText, audioBytes) = await CreateVoiceAtis(station, preset, currentAtisLetter, variables,
            cancellationToken, sandboxRequest);
        
        return new AtisBuilderVoiceAtisResponse(spokenText, audioBytes);
    }

    public async Task<string?> BuildTextAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(decodedMetar);

        var airportData = mNavDataRepository?.GetAirport(station.Identifier) ??
                          throw new AtisBuilderException($"{station.Identifier} not found in airport database.");
        
        var variables = await ParseNodesFromMetar(station, preset, decodedMetar, airportData, currentAtisLetter);
        
        return await CreateTextAtis(station, preset, currentAtisLetter, variables);
    }

    private async Task<(string?, byte[]?)> CreateVoiceAtis(AtisStation station, AtisPreset preset,
        char currentAtisLetter, List<AtisVariable> variables, CancellationToken cancellationToken,
        bool sandboxRequest = false)
    {
        var template = preset.Template ?? "";
        
        template = ReplaceContractionVariable(template, station, voiceVariable: true);

        // Custom station altimeter
        try
        {
            var matches = Regex.Matches(template, @"\[PRESSURE_(\w{4})\]", RegexOptions.IgnoreCase);
            var tasks = new List<Task<string>>();

            foreach (Match match in matches)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var icao = match.Groups[1].Value;
                    var icaoMetar = await mMetarRepository!.GetMetar(icao, triggerMessageBus: false);
                    
                    if (icaoMetar?.Pressure != null)
                    {
                        return icaoMetar.Pressure.Value?.ActualValue.ToSerialFormat() ?? "";
                    }

                    return "";
                }, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
        
            // Replace matches with results
            for (var i = 0; i < matches.Count; i++)
            {
                template = template.Replace(matches[i].Value, results[i]);
            }
        }
        catch
        {
            template = Regex.Replace(template, @"\[PRESSURE_(\w{4})\]", "", RegexOptions.IgnoreCase);
        }

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

        if (!preset.HasClosingVariable && station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement)
        {
            var voiceTemplate = station.AtisFormat.ClosingStatement.Template.Voice ?? "";
            voiceTemplate = Regex.Replace(voiceTemplate, @"{letter}", currentAtisLetter.ToString());
            voiceTemplate = Regex.Replace(voiceTemplate, @"{letter\|word}", currentAtisLetter.ToPhonetic());
            template += voiceTemplate;
        }

        var text = FormatForTextToSpeech(template.ToUpper(), station);
        text = Regex.Replace(text, @"[!?.]*([!?.])", "$1"); // clean up duplicate punctuation one last time
        text = Regex.Replace(text, "\\s+([.,!\":])", "$1");

        if (station.AtisVoice.UseTextToSpeech)
        {
            // catches multiple ATIS letter button presses in quick succession
            await Task.Delay(sandboxRequest ? 0 : 5000, cancellationToken);

            if (mTextToSpeechService == null)
                throw new AtisBuilderException("TextToSpeech service not initialized");

            var synthesizedAudio = await mTextToSpeechService.RequestAudio(text, station, cancellationToken);
            return (text, synthesizedAudio);
        }

        return (text, null);
    }

    private async Task<string> CreateTextAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        List<AtisVariable> variables)
    {
        var template = preset.Template ?? "";
        
        template = ReplaceContractionVariable(template, station, voiceVariable: false);

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

        try
        {
            var matches = Regex.Matches(template, @"\[PRESSURE_(\w{4})\]", RegexOptions.IgnoreCase);
            var tasks = new List<Task<string>>();

            foreach (Match match in matches)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var icao = match.Groups[1].Value;
                    var icaoMetar = await mMetarRepository!.GetMetar(icao, triggerMessageBus: false);

                    if (icaoMetar?.Pressure != null)
                    {
                        return icaoMetar.Pressure.Value?.ActualValue.ToString(CultureInfo.InvariantCulture) ?? "";
                    }

                    return "";
                }));
            }

            var results = await Task.WhenAll(tasks);

            // Replace matches with results
            for (var i = 0; i < matches.Count; i++)
            {
                template = template.Replace(matches[i].Value, results[i]);
            }
        }
        catch
        {
            template = Regex.Replace(template, @"\[PRESSURE_(\w{4})\]", "", RegexOptions.IgnoreCase);
        }

        template = Regex.Replace(template, @"\+([A-Z0-9]{3,4})", "$1"); // remove airports and navaid identifiers prefix
        template = Regex.Replace(template, @"\s+(?=[.,?!])", ""); // remove extra spaces before punctuation
        template = Regex.Replace(template, @"\s+", " ");
        template = Regex.Replace(template, @"(?<=\*)(-?[\,0-9]+)", "$1");
        template = Regex.Replace(template, @"(?<=\#)(-?[\,0-9]+)", "$1");
        template = Regex.Replace(template, @"\{(-?[\,0-9]+)\}", "$1");
        template = Regex.Replace(template, @"(?<=\+)([A-Z]{3})", "$1");
        template = Regex.Replace(template, @"(?<=\+)([A-Z]{4})", "$1");
        template = Regex.Replace(template, @"\*", "");
        // strip caret from runway parsing
        template = Regex.Replace(template, @"(?<![\w\d])\^((?:0?[1-9]|[1-2][0-9]|3[0-6])(?:[LRC]?))(?![\w\d])", "$1");

        if (!preset.HasClosingVariable && station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement)
        {
            var closingTemplate = station.AtisFormat.ClosingStatement.Template.Text ?? "";
            closingTemplate = Regex.Replace(closingTemplate, @"{letter}", currentAtisLetter.ToString());
            closingTemplate = Regex.Replace(closingTemplate, @"{letter\|word}", currentAtisLetter.ToPhonetic());
            template += closingTemplate;
        }

        return template;
    }

    public async Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter,
        CancellationToken cancellationToken)
    {
        if (Debugger.IsAttached)
            return;

        if (string.IsNullOrEmpty(station.IdsEndpoint))
            return;

        var request = new IdsUpdateRequest
        {
            Facility = station.Identifier,
            Preset = preset.Name ?? "",
            AtisLetter = currentAtisLetter.ToString(),
            AirportConditions = preset.AirportConditions?.StripNewLineChars() ?? "",
            Notams = preset.Notams?.StripNewLineChars() ?? "",
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "",
            AtisType = station.AtisType.ToString().ToLowerInvariant()
        };

        try
        {
            ArgumentNullException.ThrowIfNull(mDownloader);

            var jsonSerialized = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.IdsUpdateRequest);
            await mDownloader.PostJson(station.IdsEndpoint, jsonSerialized);
        }
        catch (OperationCanceledException)
        {
            //ignored
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex.ToString());
        }
        catch (Exception ex)
        {
            throw new AtisBuilderException($"Failed to Update IDS: " + ex.Message);
        }
    }

    private async Task<List<AtisVariable>> ParseNodesFromMetar(AtisStation station, AtisPreset preset, DecodedMetar metar, Airport airportData,
        char currentAtisLetter)
    {
        var time = NodeParser.Parse<ObservationTimeNode, string>(metar, station);
        var surfaceWind = NodeParser.Parse<SurfaceWindNode, SurfaceWind>(metar, station);
        var rvr = NodeParser.Parse<RunwayVisualRangeNode, RunwayVisualRange>(metar, station);
        var visibility = NodeParser.Parse<PrevailingVisibilityNode, Visibility>(metar, station);
        var presentWeather = NodeParser.Parse<PresentWeatherNode, WeatherPhenomenon>(metar, station);
        var clouds = NodeParser.Parse<CloudNode, CloudLayer>(metar, station);
        var temp = NodeParser.Parse<TemperatureNode, Value>(metar, station);
        var dew = NodeParser.Parse<DewpointNode, Value>(metar, station);
        var pressure = await NodeParser.Parse<AltimeterSettingNode, Value>(metar, station,
            mMetarRepository ?? throw new InvalidOperationException());
        var trends = NodeParser.Parse<TrendNode, TrendForecast>(metar, station);
        var recentWeather = NodeParser.Parse<RecentWeatherNode, WeatherPhenomenon>(metar, station);

        var completeWxStringVoice =
            $"{surfaceWind.VoiceAtis} {visibility.VoiceAtis} {rvr.VoiceAtis} {presentWeather.VoiceAtis} {clouds.VoiceAtis} {temp.VoiceAtis} {dew.VoiceAtis} {pressure.VoiceAtis}";
        var completeWxStringAcars =
            $"{surfaceWind.TextAtis} {visibility.TextAtis} {rvr.TextAtis} {presentWeather.TextAtis} {clouds.TextAtis} {temp.TextAtis}{(!string.IsNullOrEmpty(temp.TextAtis) || !string.IsNullOrEmpty(dew.TextAtis) ? "/" : "")}{dew.TextAtis} {pressure.TextAtis}";

        var airportConditions = "";
        if (!string.IsNullOrEmpty(preset.AirportConditions) || station.AirportConditionDefinitions.Any(x => x.Enabled))
        {
            if (station.AirportConditionsBeforeFreeText)
            {
                airportConditions = string.Join(" ",
                    string.Join(". ", station.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)),
                    preset.AirportConditions);
            }
            else
            {
                airportConditions = string.Join(" ", preset.AirportConditions,
                    string.Join(". ", station.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)));
            }
        }

        // clean up duplicate punctuation
        airportConditions = Regex.Replace(airportConditions, @"[!?.]*([!?.])", "$1");
        airportConditions = Regex.Replace(airportConditions, "\\s+([.,!\":])", "$1");

        // replace contraction variables
        var airportConditionsText = ReplaceContractionVariable(airportConditions, station, voiceVariable: false);
        var airportConditionsVoice = ReplaceContractionVariable(airportConditions, station, voiceVariable: true);

        var notams = "";
        var notamsText = "";
        var notamsVoice = "";
        if (!string.IsNullOrEmpty(preset.Notams) || station.NotamDefinitions.Any(x => x.Enabled))
        {
            if (station.NotamsBeforeFreeText)
            {
                notams += string.Join(". ",
                    string.Join(". ", station.NotamDefinitions.Where(x => x.Enabled).Select(t => t.Text)),
                    preset.Notams);
            }
            else
            {
                notams += string.Join(". ", preset.Notams,
                    string.Join(". ", station.NotamDefinitions.Where(x => x.Enabled).Select(t => t.Text)));
            }
        
            // strip extraneous punctuation
            notams = Regex.Replace(notams, @"[!?.]*([!?.])", "$1");
            notams = Regex.Replace(notams, "\\s+([.,!\":])", "$1");

            if (station.UseNotamPrefix)
            {
                notamsText += "NOTAMS... ";
                notamsVoice += $"{(station.IsFaaAtis ? "Notices to air missions" : "Notices to airmen")}: ";
            }

            if (!string.IsNullOrEmpty(notams))
            {
                notamsText += $"{notams} ";
                notamsVoice += $"{notams} ";
            }
        
            // translate contraction variables
            notamsText = ReplaceContractionVariable(notamsText, station, voiceVariable: false);
            notamsVoice = ReplaceContractionVariable(notamsVoice, station, voiceVariable: true);
        }

        var variables = new List<AtisVariable>
        {
            new("FACILITY", airportData.Id, airportData.Name),
            new("ATIS_LETTER", currentAtisLetter.ToString(), currentAtisLetter.ToPhonetic(), ["LETTER", "ATIS_CODE", "ID"]),
            new("TIME", time.TextAtis, time.VoiceAtis, ["OBS_TIME", "OBSTIME"]),
            new("WIND", surfaceWind.TextAtis, surfaceWind.VoiceAtis, ["SURFACE_WIND"]),
            new("RVR", rvr.TextAtis, rvr.VoiceAtis),
            new("VIS", visibility.TextAtis, visibility.VoiceAtis, ["PREVAILING_VISIBILITY"]),
            new("PRESENT_WX", presentWeather.TextAtis, presentWeather.VoiceAtis, ["PRESENT_WEATHER"]),
            new("CLOUDS", clouds.TextAtis, clouds.VoiceAtis),
            new("TEMP", temp.TextAtis, temp.VoiceAtis),
            new("DEW", dew.TextAtis, dew.VoiceAtis),
            new("PRESSURE", pressure.TextAtis, pressure.VoiceAtis, ["QNH"]),
            new("WX", completeWxStringAcars, completeWxStringVoice, ["FULL_WX_STRING"]),
            new("ARPT_COND", airportConditionsText, airportConditionsVoice, ["ARRDEP"]),
            new("NOTAMS", notamsText, notamsVoice),
            new("TREND", trends.TextAtis, trends.VoiceAtis),
            new("RECENT_WX", recentWeather.TextAtis, recentWeather.VoiceAtis)
        };

        if (!station.IsFaaAtis)
        {
            var trl = station.AtisFormat.TransitionLevel.Values.FirstOrDefault(t =>
                metar.Pressure?.Value?.ActualValue >= t.Low && metar.Pressure.Value?.ActualValue <= t.High);

            if (trl != null)
            {
                var trlTemplateText = station.AtisFormat.TransitionLevel.Template.Text;
                if (trlTemplateText != null)
                {
                    trlTemplateText = Regex.Replace(trlTemplateText, @"{trl}", trl.Altitude.ToString());
                    trlTemplateText = Regex.Replace(trlTemplateText, @"{trl\|text}", trl.Altitude.ToSerialFormat());
                }

                var trlTemplateVoice = station.AtisFormat.TransitionLevel.Template.Voice;
                if (trlTemplateVoice != null)
                {
                    trlTemplateVoice = Regex.Replace(trlTemplateVoice, @"{trl}", trl.Altitude.ToString());
                    trlTemplateVoice = Regex.Replace(trlTemplateVoice, @"{trl\|text}", trl.Altitude.ToSerialFormat());
                }

                variables.Add(new AtisVariable("TL", trlTemplateText ?? "", trlTemplateVoice ?? ""));
            }
            else
            {
                variables.Add(new AtisVariable("TL", "", ""));
            }
        }

        var closingTextTemplate = station.AtisFormat.ClosingStatement.Template.Text;
        var closingVoiceTemplate = station.AtisFormat.ClosingStatement.Template.Voice;

        if (!string.IsNullOrEmpty(closingTextTemplate) && !string.IsNullOrEmpty(closingVoiceTemplate))
        {
            closingTextTemplate = Regex.Replace(closingTextTemplate, @"{letter}", currentAtisLetter.ToString());
            closingTextTemplate = Regex.Replace(closingTextTemplate, @"{letter\|word}", currentAtisLetter.ToPhonetic());

            closingVoiceTemplate = Regex.Replace(closingVoiceTemplate, @"{letter}", currentAtisLetter.ToString());
            closingVoiceTemplate =
                Regex.Replace(closingVoiceTemplate, @"{letter\|word}", currentAtisLetter.ToPhonetic());

            variables.Add(new AtisVariable("CLOSING", closingTextTemplate, closingVoiceTemplate));
        }
        else
        {
            variables.Add(new AtisVariable("CLOSING", "", ""));
        }

        return variables;
    }

    private string FormatForTextToSpeech(string input, AtisStation station)
    {
        input = Regex.Replace(input, @"@([A-Z]+_?)+", match =>
        {
            var key = match.Value; // Get the matched variable
            var variable = station.Contractions.Find(v => v.VariableName == key); // Find matching variable
            return variable != null ? variable.Voice ?? "" : ""; // Replace or remove if not found
        });

        // airports and navaid identifiers
        var navdataMatches = Regex.Matches(input, @"\+([A-Z0-9]{3,4})");
        if (navdataMatches.Count > 0)
        {
            foreach (Match match in navdataMatches)
            {
                if (match.Groups.Count == 0)
                    continue;

                var navaid = mNavDataRepository?.GetNavaid(match.Groups[1].Value);
                if (navaid != null)
                {
                    input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(match.Value)}(?![\w\d])", navaid.Name);
                }
                else
                {
                    var airport = mNavDataRepository?.GetAirport(match.Groups[1].Value);
                    if (airport != null)
                    {
                        input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(match.Value)}(?![\w\d])",
                            airport.Name);
                    }
                }
            }
        }

        // parse zulu times
        input = Regex.Replace(input, @"([0-9])([0-9])([0-9])([0-8])Z",
            m => string.Format($"{int.Parse(m.Groups[1].Value).ToSerialFormat()} " +
                               $"{int.Parse(m.Groups[2].Value).ToSerialFormat()} " +
                               $"{int.Parse(m.Groups[3].Value).ToSerialFormat()} " +
                               $"{int.Parse(m.Groups[4].Value).ToSerialFormat()} zulu"));

        // vhf frequencies
        input = Regex.Replace(input, @"(1\d\d\.\d\d?\d?)",
            m => m.Groups[1].Value.ToSerialFormat(station.UseDecimalTerminology) ?? string.Empty);

        // letters
        input = Regex.Replace(input, @"\*([A-Z]{1,2}[0-9]{0,2})", m => m.Value.ToAlphaNumericWordGroup()).Trim();

        // parse taxiways
        input = Regex.Replace(input, @"\bTWY ([A-Z]{1,2}[0-9]{0,2})\b",
            m => $"TWY {m.Groups[1].Value.ToAlphaNumericWordGroup()}");
        input = Regex.Replace(input, @"\bTWYS ([A-Z]{1,2}[0-9]{0,2})\b",
            m => $"TWYS {m.Groups[1].Value.ToAlphaNumericWordGroup()}");

        // parse runways
        input = Regex.Replace(input, @"\b(RY|RWY|RWYS|RUNWAY|RUNWAYS)\s?([0-9]{1,2})([LRC]?)\b",
            m => StringExtensions.RwyNumbersToWords(int.Parse(m.Groups[2].Value), m.Groups[3].Value,
                prefix: !string.IsNullOrEmpty(m.Groups[1].Value),
                plural: !string.IsNullOrEmpty(m.Groups[1].Value) &&
                        (m.Groups[1].Value == "RWYS" || m.Groups[1].Value == "RUNWAYS"),
                leadingZero: !station.IsFaaAtis));

        // parse individual runway: ^18R, ^01C, ^36
        var runwayMatches = Regex.Matches(input, @"\^(0[1-9]|1[0-9]|2[0-9]|3[0-6]|[1-9])([RLC]?)");
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

                var replace = int.Parse(rwy.Groups[1].Value).ToSerialFormat(leadingZero: !station.IsFaaAtis) + " " +
                              designator;
                input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(rwy.Value)}(?![\w\d])", replace.Trim());
            }
        }

        // read numbers in group format, prefixed with # or surrounded with {}
        input = Regex.Replace(input, @"\*(-?[\,0-9]+)",
            m => int.Parse(m.Groups[1].Value.Replace(",", "")).ToGroupForm());
        input = Regex.Replace(input, @"\{(-?[\,0-9]+)\}",
            m => int.Parse(m.Groups[1].Value.Replace(",", "")).ToGroupForm());

        // read numbers in serial format
        input = Regex.Replace(input, @"([+-])?([0-9]+\.[0-9]+|[0-9]+|\.[0-9]+)(?![^{]*\})",
            m => m.Value.ToSerialFormat(station.UseDecimalTerminology) ?? string.Empty);

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
    
    private static string ReplaceContractionVariable(string text, AtisStation station, bool voiceVariable = true)
    {
        return Regex.Replace(text, @"\@([\w]+(?:_[\w]+)*)", match =>
        {
            var key = match.Groups[1].Value; // Get the matched variable
            var variable = station.Contractions.Find(v => v.VariableName == key); // Find matching variable
            return variable != null ? (voiceVariable ? variable.Voice : variable.Text) ?? "" : ""; // Replace or remove if not found
        });
    }
}