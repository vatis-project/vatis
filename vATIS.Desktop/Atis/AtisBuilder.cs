// <copyright file="AtisBuilder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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
using Serilog;
using Vatsim.Network;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Container;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// The ATIS builder that builds ATIS messages in text and voice formats.
/// </summary>
public class AtisBuilder : IAtisBuilder
{
    private readonly IDownloader? _downloader;
    private readonly IMetarRepository? _metarRepository;
    private readonly INavDataRepository? _navDataRepository;
    private readonly ITextToSpeechService? _textToSpeechService;
    private readonly IClientAuth _clientAuth;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisBuilder"/> class.
    /// </summary>
    /// <param name="downloader">The downloader.</param>
    /// <param name="navDataRepository">The navigation data repository.</param>
    /// <param name="textToSpeechService">The text to speech service.</param>
    /// <param name="metarRepository">The METAR repository.</param>
    /// <param name="clientAuth">The client auth service.</param>
    public AtisBuilder(IDownloader downloader, INavDataRepository navDataRepository,
        ITextToSpeechService textToSpeechService, IMetarRepository metarRepository, IClientAuth clientAuth)
    {
        _downloader = downloader;
        _metarRepository = metarRepository;
        _navDataRepository = navDataRepository;
        _textToSpeechService = textToSpeechService;
        _clientAuth = clientAuth;
    }

    /// <inheritdoc/>
    public async Task<AtisBuilderVoiceAtisResponse> BuildVoiceAtis(AtisStation station, AtisPreset preset, char currentAtisLetter, DecodedMetar decodedMetar,
        CancellationToken cancellationToken, bool sandboxRequest = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(decodedMetar);

        var airportData = _navDataRepository?.GetAirport(station.Identifier) ??
                          throw new AtisBuilderException($"{station.Identifier} not found in airport database.");

        var variables = await ParseNodesFromMetar(station, preset, decodedMetar, airportData, currentAtisLetter);

        var (spokenText, audioBytes) = await CreateVoiceAtis(station, preset, currentAtisLetter, variables,
            cancellationToken, sandboxRequest);

        return new AtisBuilderVoiceAtisResponse(spokenText, audioBytes);
    }

    /// <inheritdoc/>
    public async Task<string?> BuildTextAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(decodedMetar);

        var airportData = _navDataRepository?.GetAirport(station.Identifier) ??
                          throw new AtisBuilderException($"{station.Identifier} not found in airport database.");

        var variables = await ParseNodesFromMetar(station, preset, decodedMetar, airportData, currentAtisLetter);

        return await CreateTextAtis(station, preset, currentAtisLetter, variables);
    }

    /// <inheritdoc/>
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
            ArgumentNullException.ThrowIfNull(_downloader);

            string? jwt = null;
            if (!ServiceProvider.IsDevelopmentEnvironment() && !string.IsNullOrEmpty(_clientAuth.IdsValidationKey()))
            {
                // Generate a signed JWT token for optional validation by the IDS server.
                jwt = JwtHelper.GenerateJwt(_clientAuth.IdsValidationKey(), "ids-validation");
            }

            var jsonSerialized = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.IdsUpdateRequest);
            await _downloader.PostJson(station.IdsEndpoint, jsonSerialized, jwt);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex.ToString(), "HttpRequestException updating IDS");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update IDS");
            throw new AtisBuilderException($"Failed to Update IDS: " + ex.Message);
        }
    }

    private static string ReplaceContractionVariable(string text, AtisStation station, bool voiceVariable = true)
    {
        return Regex.Replace(text, @"\@?([\w]+(?:_[\w]+)*)", match =>
        {
            var key = match.Groups[1].Value; // Get the matched variable
            var variable = station.Contractions.Find(v => v.VariableName == key); // Find matching variable

            if (variable != null)
            {
                // Replace with the voice or text variable value if found, otherwise leave as is
                return (voiceVariable ? variable.Voice : variable.Text) ?? match.Value;
            }

            // Return the match as is if no variable is found
            return match.Value;
        });
    }

    private static string RemoveTextParsingCharacters(string text)
    {
        text = Regex.Replace(text, @"\+([A-Z0-9]{3,4})", "$1"); // remove airports and navaid identifiers prefix
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"(?<=\*)(-?[\,0-9]+)", "$1");
        text = Regex.Replace(text, @"(?<=\#)(-?[\,0-9]+)", "$1");
        text = Regex.Replace(text, @"\{(-?[\,0-9]+)\}", "$1");
        text = Regex.Replace(text, @"(?<=\+)([A-Z]{3})", "$1");
        text = Regex.Replace(text, @"(?<=\+)([A-Z]{4})", "$1");
        text = Regex.Replace(text, @"\*", "");

        // strip caret from runway parsing
        text = Regex.Replace(text, @"(?<![\w\d])\^((?:0?[1-9]|[1-2][0-9]|3[0-6])(?:[LRC]?))(?![\w\d])", "$1");

        return text;
    }

    private async Task<(string? SpokenText, byte[]? AudioBytes)> CreateVoiceAtis(AtisStation station, AtisPreset preset,
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
                    var icaoMetar = await _metarRepository!.GetMetar(icao, triggerMessageBus: false);

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

            if (_textToSpeechService == null)
                throw new AtisBuilderException("TextToSpeech service not initialized");

            var synthesizedAudio = await _textToSpeechService.RequestAudio(text, station, cancellationToken);
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
                    var icaoMetar = await _metarRepository!.GetMetar(icao, triggerMessageBus: false);

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

        if (!preset.HasClosingVariable && station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement)
        {
            var closingTemplate = station.AtisFormat.ClosingStatement.Template.Text ?? "";
            closingTemplate = Regex.Replace(closingTemplate, @"{letter}", currentAtisLetter.ToString());
            closingTemplate = Regex.Replace(closingTemplate, @"{letter\|word}", currentAtisLetter.ToPhonetic());
            template += closingTemplate;
        }

        // Remove text parsing characters
        template = RemoveTextParsingCharacters(template);

        return template;
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
            _metarRepository ?? throw new InvalidOperationException());
        var trends = NodeParser.Parse<TrendNode, TrendForecast>(metar, station);
        var recentWeather = NodeParser.Parse<RecentWeatherNode, WeatherPhenomenon>(metar, station);
        var windshear = NodeParser.Parse<WindShearNode, string>(metar, station);

        var completeWxStringVoice =
            $"{surfaceWind.VoiceAtis} {visibility.VoiceAtis} {rvr.VoiceAtis} {presentWeather.VoiceAtis} {clouds.VoiceAtis} {temp.VoiceAtis} {dew.VoiceAtis} {pressure.VoiceAtis} {recentWeather.VoiceAtis} {windshear.VoiceAtis} {trends.VoiceAtis}";
        var completeWxStringAcars =
            $"{surfaceWind.TextAtis} {visibility.TextAtis} {rvr.TextAtis} {presentWeather.TextAtis} {clouds.TextAtis} {temp.TextAtis}{(!string.IsNullOrEmpty(temp.TextAtis) || !string.IsNullOrEmpty(dew.TextAtis) ? "/" : "")}{dew.TextAtis} {pressure.TextAtis} {recentWeather.TextAtis} {windshear.TextAtis} {trends.TextAtis}";

        var airportConditions = "";
        if (!string.IsNullOrEmpty(preset.AirportConditions) || station.AirportConditionDefinitions.Any(x => x.Enabled))
        {
            if (station.AirportConditionsBeforeFreeText)
            {
                airportConditions = string.Join(" ", new[]
                {
                    string.Join(". ", station.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)),
                    preset.AirportConditions
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                airportConditions = string.Join(" ", new[]
                {
                    preset.AirportConditions,
                    string.Join(". ", station.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text))
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        // clean up duplicate punctuation
        airportConditions = Regex.Replace(airportConditions, @"[!?.]*([!?.])", "$1");
        airportConditions = Regex.Replace(airportConditions, "\\s+([.,!\":])", "$1");

        // replace contraction variables
        var airportConditionsText = ReplaceContractionVariable(airportConditions, station, voiceVariable: false);
        var airportConditionsVoice = ReplaceContractionVariable(airportConditions, station, voiceVariable: true);

        // remove text parsing characters
        airportConditionsText = RemoveTextParsingCharacters(airportConditionsText);

        var notams = "";
        var notamsText = "";
        var notamsVoice = "";
        if (!string.IsNullOrEmpty(preset.Notams) || station.NotamDefinitions.Any(x => x.Enabled))
        {
            if (station.NotamsBeforeFreeText)
            {
                notams += string.Join(". ", new[]
                {
                    string.Join(". ", station.NotamDefinitions.Where(x => x.Enabled).Select(t => t.Text)),
                    preset.Notams
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                notams += string.Join(". ", new[]
                {
                    preset.Notams,
                    string.Join(". ", station.NotamDefinitions.Where(x => x.Enabled).Select(t => t.Text))
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            // strip extraneous punctuation
            notams = Regex.Replace(notams, @"[!?.]*([!?.])", "$1");
            notams = Regex.Replace(notams, "\\s+([.,!\":])", "$1");

            // Add space to end of NOTAMs
            notams = notams.Trim() + " ";
        }

        if (!string.IsNullOrEmpty(notams))
        {
            var template = station.AtisFormat.Notams.Template;
            if (template.Text != null)
            {
                notamsText = Regex.Replace(template.Text, "{notams}", notams, RegexOptions.IgnoreCase);

                // replace contraction variables
                notamsText = ReplaceContractionVariable(notamsText, station, voiceVariable: false);

                // remove text parsing characters
                notamsText = RemoveTextParsingCharacters(notamsText);
            }

            if (template.Voice != null)
            {
                notamsVoice = Regex.Replace(template.Voice, "{notams}", notams, RegexOptions.IgnoreCase);

                // replace contraction variables
                notamsVoice = ReplaceContractionVariable(notamsVoice, station, voiceVariable: true);
            }
        }

        // Strip extra spaces from weather string.
        completeWxStringAcars = Regex.Replace(completeWxStringAcars, @"\s+", " ");
        completeWxStringVoice = Regex.Replace(completeWxStringVoice, @"\s+", " ");

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
            new("WX", completeWxStringAcars.Trim(), completeWxStringVoice, ["FULL_WX_STRING"]),
            new("ARPT_COND", airportConditionsText, airportConditionsVoice, ["ARRDEP"]),
            new("NOTAMS", notamsText, notamsVoice),
            new("TREND", trends.TextAtis, trends.VoiceAtis),
            new("RECENT_WX", recentWeather.TextAtis, recentWeather.VoiceAtis),
            new("WS", windshear.TextAtis, windshear.VoiceAtis)
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

                var navaid = _navDataRepository?.GetNavaid(match.Groups[1].Value);
                if (navaid != null)
                {
                    input = Regex.Replace(input, $@"(?<![\w\d]){Regex.Escape(match.Value)}(?![\w\d])", navaid.Name);
                }
                else
                {
                    var airport = _navDataRepository?.GetAirport(match.Groups[1].Value);
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
}
