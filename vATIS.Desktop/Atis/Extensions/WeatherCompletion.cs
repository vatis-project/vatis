// <copyright file="WeatherCompletion.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Works out what can come next in a weather condition at the caret, for auto-complete.
/// </summary>
public static partial class WeatherCompletion
{
    private static readonly (string Name, string Description)[] s_fields =
    [
        ("ceiling", "Lowest broken or overcast layer (ft, m)"),
        ("cloudbase", "Lowest cloud layer of any coverage (ft, m)"),
        ("rvr", "Lowest runway visual range (m, km, ft)"),
        ("visibility", "Prevailing visibility (m, km, ft)"),
        ("windspeed", "Mean wind speed (kt, kmh, mps)"),
        ("gust", "Wind gust speed (kt, kmh, mps)"),
        ("winddir", "Mean wind direction in degrees"),
        ("temperature", "Air temperature (c, f)"),
        ("dewpoint", "Dewpoint (c, f)"),
        ("qnh", "Pressure setting (hpa, inhg)"),
        ("depth", "Deepest runway deposit (mm)"),
        ("weather", "Present weather codes, with INCLUDES / EXCLUDES"),
        ("cloudtype", "Cloud types CB or TCU, with INCLUDES / EXCLUDES"),
        ("windshear", "True when windshear is reported"),
        ("cavok", "True when the METAR reports CAVOK"),
    ];

    private static readonly (string Text, string Description)[] s_numericOperators =
    [
        ("<", "below"),
        ("<=", "at or below"),
        (">", "above"),
        (">=", "at or above"),
        ("=", "equals"),
    ];

    private static readonly (string Text, string Description)[] s_weatherCodes =
    [
        ("TS", "thunderstorm"),
        ("TSRA", "thunderstorm with rain"),
        ("RA", "rain"),
        ("SHRA", "rain showers"),
        ("FZRA", "freezing rain"),
        ("DZ", "drizzle"),
        ("FZDZ", "freezing drizzle"),
        ("SN", "snow"),
        ("SHSN", "snow showers"),
        ("BLSN", "blowing snow"),
        ("SG", "snow grains"),
        ("PL", "ice pellets"),
        ("GR", "hail"),
        ("GS", "small hail"),
        ("FG", "fog"),
        ("FZFG", "freezing fog"),
        ("BR", "mist"),
        ("HZ", "haze"),
        ("FU", "smoke"),
        ("VA", "volcanic ash"),
        ("DU", "dust"),
        ("SA", "sand"),
        ("SQ", "squalls"),
        ("FC", "funnel cloud"),
        ("DS", "duststorm"),
        ("SS", "sandstorm"),
    ];

    private static readonly (string Text, string Description)[] s_cloudTypes =
    [
        ("CB", "cumulonimbus"),
        ("TCU", "towering cumulus"),
    ];

    private enum Expecting
    {
        Operand,
        NumericOperator,
        TextOperator,
        Number,
        AfterNumber,
        Codes,
        AfterCondition,
        Nothing,
    }

    /// <summary>
    /// Gets the suggestions for the position of the caret.
    /// </summary>
    /// <param name="text">The condition typed so far.</param>
    /// <param name="caret">The caret offset in the text.</param>
    /// <returns>The suggestions, which may be empty.</returns>
    public static CompletionSuggestionResult GetSuggestions(string? text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Clamp(caret, 0, text.Length);
        var prefix = text[..caret];

        // Inside an open quote the codes of a text field are suggested, one word at a time.
        var quote = prefix.LastIndexOf('"');
        if (quote >= 0 && prefix.Count(c => c == '"') % 2 == 1)
        {
            var wordStart = Math.Max(prefix.LastIndexOfAny([' ', ',']) + 1, quote + 1);
            return Filter(wordStart, prefix[wordStart..], CodesFor(Analyze(prefix[..quote]).Field));
        }

        var partialStart = caret;
        while (partialStart > 0 && IsWordChar(prefix[partialStart - 1]))
        {
            partialStart--;
        }

        // A unit can follow a number directly, e.g. "200f".
        var number = NumberPrefix().Match(prefix[partialStart..]);
        if (number.Success)
        {
            partialStart += number.Length;
        }

        var state = Analyze(prefix[..partialStart]);
        return Filter(partialStart, prefix[partialStart..], For(state));
    }

    private static bool IsWordChar(char c)
    {
        return !char.IsWhiteSpace(c) && c is not ('(' or ')' or '<' or '>' or '=' or '"');
    }

    private static CompletionSuggestionResult Filter(int start, string partial, IEnumerable<CompletionSuggestion> candidates)
    {
        var items = candidates
            .Where(x => x.Text.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new CompletionSuggestionResult(start, items);
    }

    private static IEnumerable<CompletionSuggestion> CodesFor(WeatherField? field)
    {
        var codes = field == WeatherField.CloudType ? s_cloudTypes : field == WeatherField.PresentWeather ? s_weatherCodes : [];
        return codes.Select(x => new CompletionSuggestion(x.Text, x.Text, x.Description, ContinueAfter: false));
    }

    private static IEnumerable<CompletionSuggestion> For((Expecting Expecting, WeatherField? Field) state)
    {
        switch (state.Expecting)
        {
            case Expecting.Operand:
                foreach (var (name, description) in s_fields)
                {
                    yield return new CompletionSuggestion(name, name + " ", description);
                }

                yield return new CompletionSuggestion("NOT", "NOT ", "reverse the next condition");
                break;
            case Expecting.NumericOperator:
                foreach (var (op, description) in s_numericOperators)
                {
                    yield return new CompletionSuggestion(op, op + " ", description, ContinueAfter: false);
                }

                break;
            case Expecting.TextOperator:
                yield return new CompletionSuggestion("INCLUDES", "INCLUDES ", "any of the codes is in the METAR");
                yield return new CompletionSuggestion("EXCLUDES", "EXCLUDES ", "none of the codes is in the METAR");
                break;
            case Expecting.AfterNumber:
                foreach (var unit in state.Field is { } f ? WeatherFieldInfo.GetUnits(f) : [])
                {
                    var name = WeatherExpression.GetUnitName(unit);
                    yield return new CompletionSuggestion(name, name + " ", "unit");
                }

                foreach (var join in Joiners())
                {
                    yield return join;
                }

                break;
            case Expecting.Codes:
                foreach (var code in CodesFor(state.Field))
                {
                    yield return code;
                }

                break;
            case Expecting.AfterCondition:
                foreach (var join in Joiners())
                {
                    yield return join;
                }

                break;
        }
    }

    private static IEnumerable<CompletionSuggestion> Joiners()
    {
        yield return new CompletionSuggestion("AND", "AND ", "both conditions must be met");
        yield return new CompletionSuggestion("OR", "OR ", "either condition may be met");
    }

    // Reads the text before the word being typed and works out what may come next.
    private static (Expecting Expecting, WeatherField? Field) Analyze(string text)
    {
        var state = Expecting.Operand;
        WeatherField? field = null;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '(')
            {
                state = Expecting.Operand;
                i++;
            }
            else if (c == ')')
            {
                state = Expecting.AfterCondition;
                i++;
            }
            else if (c is '<' or '>' or '=')
            {
                i++;
                if (i < text.Length && text[i] == '=')
                {
                    i++;
                }

                state = state == Expecting.NumericOperator ? Expecting.Number : Expecting.Nothing;
            }
            else if (c == '"')
            {
                var close = text.IndexOf('"', i + 1);
                i = close < 0 ? text.Length : close + 1;
                state = state == Expecting.Codes ? Expecting.AfterCondition : Expecting.Nothing;
            }
            else
            {
                var start = i;
                while (i < text.Length && IsWordChar(text[i]))
                {
                    i++;
                }

                (state, field) = Advance(state, field, text[start..i]);
            }
        }

        return (state, field);
    }

    private static (Expecting State, WeatherField? Field) Advance(Expecting state, WeatherField? field, string word)
    {
        var upper = word.ToUpperInvariant();
        if (upper is "AND" or "OR")
        {
            return (Expecting.Operand, null);
        }

        switch (state)
        {
            case Expecting.Operand:
                if (upper == "NOT")
                {
                    return (Expecting.Operand, null);
                }

                if (!WeatherExpression.TryGetField(word, out var f))
                {
                    return (Expecting.Nothing, null);
                }

                return WeatherFieldInfo.GetKind(f) switch
                {
                    WeatherFieldKind.Flag => (Expecting.AfterCondition, f),
                    WeatherFieldKind.Text => (Expecting.TextOperator, f),
                    _ => (Expecting.NumericOperator, f),
                };
            case Expecting.TextOperator:
                return upper is "INCLUDES" or "INCLUDE" or "CONTAINS" or "EXCLUDES" or "EXCLUDE"
                    ? (Expecting.Codes, field)
                    : (Expecting.Nothing, field);
            case Expecting.Codes:
                return (Expecting.AfterCondition, field);
            case Expecting.Number:
                var match = NumberPrefix().Match(word);
                return !match.Success ? (Expecting.Nothing, field) : match.Length < word.Length ? (Expecting.AfterCondition, field) : (Expecting.AfterNumber, field);
            case Expecting.AfterNumber:
                return (Expecting.AfterCondition, field);
            default:
                return (Expecting.Nothing, field);
        }
    }

    [GeneratedRegex(@"^[+-]?\d+(\.\d+)?")]
    private static partial Regex NumberPrefix();
}
