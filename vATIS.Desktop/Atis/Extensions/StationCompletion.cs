// <copyright file="StationCompletion.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Works out which keywords can come next in a station condition at the caret, for auto-complete.
/// </summary>
public static class StationCompletion
{
    private static readonly CompletionSuggestion[] s_operators =
    [
        new("AND", "AND ", "both must be online", ContinueAfter: false),
        new("OR", "OR ", "either may be online", ContinueAfter: false),
        new("NOT", "NOT ", "must not be online", ContinueAfter: false),
    ];

    private static readonly CompletionSuggestion[] s_operand =
    [
        new("NOT", "NOT ", "must not be online", ContinueAfter: false),
    ];

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

        var partialStart = caret;
        while (partialStart > 0 && IsWordChar(prefix[partialStart - 1]))
        {
            partialStart--;
        }

        var candidates = AfterOperand(prefix[..partialStart]) ? s_operators : s_operand;
        var partial = prefix[partialStart..];
        var items = candidates
            .Where(x => x.Text.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new CompletionSuggestionResult(partialStart, items);
    }

    private static bool IsWordChar(char c)
    {
        return !char.IsWhiteSpace(c) && c is not ('(' or ')' or ',' or ';');
    }

    // True when the text before the word being typed ends with a callsign or a closing parenthesis, so an operator
    // may follow. At the start, or after an operator, an opening parenthesis or a comma, a callsign comes next.
    private static bool AfterOperand(string text)
    {
        var afterOperand = false;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is '(' or ',' or ';')
            {
                afterOperand = false;
                i++;
            }
            else if (c == ')')
            {
                afterOperand = true;
                i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else
            {
                var start = i;
                while (i < text.Length && IsWordChar(text[i]))
                {
                    i++;
                }

                afterOperand = !text[start..i].Equals("AND", StringComparison.OrdinalIgnoreCase) &&
                               !text[start..i].Equals("OR", StringComparison.OrdinalIgnoreCase) &&
                               !text[start..i].Equals("NOT", StringComparison.OrdinalIgnoreCase);
            }
        }

        return afterOperand;
    }
}
