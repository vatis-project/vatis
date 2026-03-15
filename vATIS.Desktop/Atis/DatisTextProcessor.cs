// <copyright file="DatisTextProcessor.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

/// <inheritdoc cref="IDatisTextProcessor"/>
public sealed class DatisTextProcessor : IDatisTextProcessor
{
    private const string NotamDelimiter = "NOTAMS... ";

    /// <inheritdoc />
    public DatisResult Process(
        string rawBody,
        string stationId,
        char? atisLetter,
        List<ContractionMeta> contractions,
        List<DatisTextReplacement> replacements,
        string prependAirportConditions,
        string appendAirportConditions,
        string prependNotams,
        string appendNotams)
    {
        var text = StripEnvelope(rawBody);
        text = NormalizeNotamDelimiters(text);
        text = ApplyReplacements(text, replacements);
        text = NormalizeWhitespace(text);
        text = CleanPunctuation(text);

        var (airportConditions, notams) = SplitConditionsAndNotams(text);

        airportConditions = ApplyPrependAppend(airportConditions, prependAirportConditions, appendAirportConditions);
        notams = ApplyPrependAppend(notams, prependNotams, appendNotams);

        airportConditions = ExpandContractions(airportConditions, contractions);
        notams = ExpandContractions(notams, contractions);

        return new DatisResult(stationId, airportConditions, notams, atisLetter);
    }

    /// <summary>
    /// Removes the first two sentences (identification envelope) and trailing
    /// "ADVS YOU HAVE..." text from the D-ATIS body.
    /// </summary>
    private static string StripEnvelope(string text)
    {
        // Remove first 2 sentences (e.g. "KJFK ATIS INFO A. 0153Z." or similar)
        var sentences = text.Split(". ", 3, StringSplitOptions.None);
        if (sentences.Length > 2)
        {
            text = sentences[2];
        }

        // Remove trailing "ADVS YOU HAVE..." onward
        text = Regex.Replace(text, @" \.{0,3}ADVS YOU HAVE.*", string.Empty);

        return text;
    }

    /// <summary>
    /// Normalizes various NOTAM header variants into a single delimiter.
    /// </summary>
    private static string NormalizeNotamDelimiters(string text)
    {
        text = text.Replace("NOTICE TO AIR MISSIONS, NOTAMS. ", NotamDelimiter);
        text = text.Replace("NOTICE TO AIR MISSIONS. ", NotamDelimiter);
        text = text.Replace("NOTICE TO AIR MEN. ", NotamDelimiter);
        text = text.Replace("NOTICE TO AIRMEN. ", NotamDelimiter);
        text = text.Replace("NOTAMS. ", NotamDelimiter);
        text = text.Replace("NOTAM. ", NotamDelimiter);
        return text;
    }

    /// <summary>
    /// Applies user-defined replacement rules. For regex patterns, also strips an optional
    /// trailing punctuation character after the match.
    /// </summary>
    private static string ApplyReplacements(string text, List<DatisTextReplacement> replacements)
    {
        foreach (var replacement in replacements)
        {
            if (string.IsNullOrEmpty(replacement.Pattern))
            {
                continue;
            }

            try
            {
                if (replacement.IsRegex)
                {
                    text = Regex.Replace(
                        text,
                        replacement.Pattern + @"[,.;]{0,1}",
                        replacement.Replacement,
                        RegexOptions.None,
                        TimeSpan.FromSeconds(1));
                }
                else
                {
                    text = text.Replace(replacement.Pattern, replacement.Replacement);
                }
            }
            catch (RegexParseException ex)
            {
                Log.Warning(ex, "Invalid D-ATIS replacement regex pattern: {Pattern}", replacement.Pattern);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid D-ATIS replacement value for regex pattern: {Pattern}", replacement.Pattern);
            }
            catch (RegexMatchTimeoutException ex)
            {
                Log.Warning(ex, "Timed out applying D-ATIS replacement regex pattern: {Pattern}", replacement.Pattern);
            }
        }

        return text;
    }

    /// <summary>
    /// Collapses multiple consecutive whitespace characters into a single space and trims.
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    /// <summary>
    /// Fixes double periods, spacing around punctuation, and other punctuation artifacts.
    /// </summary>
    private static string CleanPunctuation(string text)
    {
        // Preserve triple dots by temporarily replacing them
        text = text.Replace("...", "/./");
        text = text.Replace("..", ".");
        text = text.Replace("/./", "...");

        text = text.Replace("  ", " ");
        text = text.Replace(" . ", ". ");
        text = text.Replace(", ,", ",");
        text = text.Replace(" ; ", "; ");
        text = text.Replace(" .,", " ,");
        text = text.Replace(" , ", ", ");
        text = text.Replace("., ", ", ");
        text = text.Replace("&amp;", "&");
        text = text.Replace(" ;.", ".");
        text = text.Replace(" ;,", ",");

        return text;
    }

    /// <summary>
    /// Replaces contraction text with template variable references (e.g. <c>@VariableName</c>)
    /// so that the vATIS template engine expands them. Contractions are sorted by text length
    /// descending to avoid partial matches. Digit-only contractions are skipped.
    /// </summary>
    private static string ExpandContractions(string text, List<ContractionMeta> contractions)
    {
        var sorted = contractions
            .Where(c => !string.IsNullOrEmpty(c.Text) && !string.IsNullOrEmpty(c.VariableName))
            .Where(c => !c.Text!.All(char.IsDigit))
            .OrderByDescending(c => c.Text!.Length)
            .ToList();

        foreach (var contraction in sorted)
        {
            var pattern = @"(?<!@)\b" + Regex.Escape(contraction.Text!) + @"\b";
            var variable = "@" + contraction.VariableName;

            // Replace at word boundaries followed by common punctuation or space
            text = Regex.Replace(text, pattern + @"(?=[,.\s;]|$)", variable);
        }

        return text;
    }

    /// <summary>
    /// Prepends and/or appends user-defined text to a section, separated by spaces.
    /// </summary>
    private static string ApplyPrependAppend(string text, string prepend, string append)
    {
        if (!string.IsNullOrWhiteSpace(prepend))
        {
            text = prepend.Trim() + " " + text;
        }

        if (!string.IsNullOrWhiteSpace(append))
        {
            text = text + " " + append.Trim();
        }

        return text.Trim();
    }

    /// <summary>
    /// Splits text on the NOTAM delimiter into airport conditions and NOTAMs sections.
    /// </summary>
    private static (string AirportConditions, string Notams) SplitConditionsAndNotams(string text)
    {
        var delimiterIndex = text.IndexOf(NotamDelimiter, StringComparison.Ordinal);
        if (delimiterIndex >= 0)
        {
            var conditions = text[..delimiterIndex].Trim();
            var notams = text[(delimiterIndex + NotamDelimiter.Length)..].Trim();
            return (conditions, notams);
        }

        return (text.Trim(), string.Empty);
    }
}
