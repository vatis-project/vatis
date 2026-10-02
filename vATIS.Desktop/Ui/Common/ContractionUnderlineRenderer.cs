// <copyright file="ContractionUnderlineRenderer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// A background renderer that underlines contraction variables and built-in speech tokens (such as runways and
/// taxiways). The underline is drawn as a single continuous line across multi-word tokens.
/// </summary>
public partial class ContractionUnderlineRenderer : IBackgroundRenderer
{
    private const double UnderlineGap = 3;

    private static readonly Pen UnderlinePen = new(Brushes.White, 1, new DashStyle([1, 2], 0));

    private readonly Func<string, bool> _isContraction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractionUnderlineRenderer"/> class.
    /// </summary>
    /// <param name="isContraction">Returns whether a word is a known contraction.</param>
    public ContractionUnderlineRenderer(Func<string, bool> isContraction)
    {
        _isContraction = isContraction;
    }

    /// <inheritdoc/>
    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>
    /// Matches the built-in tokens that are rewritten into spoken words when building the voice ATIS.
    /// </summary>
    /// <returns>The built-in token regex.</returns>
    [GeneratedRegex(@"\+[A-Z0-9]{3,4}\b|\*[A-Z]{1,2}[0-9]{0,2}\b|\^(?:0[1-9]|[12][0-9]|3[0-6]|[1-9])[RLC]?\b|\*-?[\,0-9]+|\{-?[\,0-9]+\}|\b(?:RY|RWYS?|RUNWAYS?)\s?[0-9]{1,2}[LRC]?\b|\bTWYS? [A-Z]{1,2}[0-9]{0,2}\b|&")]
    public static partial Regex BuiltInTokenRegex();

    /// <inheritdoc/>
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid)
            return;

        var document = textView.Document;
        if (document == null)
            return;

        foreach (var visualLine in textView.VisualLines)
        {
            var line = visualLine.FirstDocumentLine;
            if (line.Length == 0)
                continue;

            var text = document.GetText(line);
            var segments = new List<(int Start, int End)>();
            foreach (Match match in BuiltInTokenRegex().Matches(text))
                segments.Add((match.Index, match.Index + match.Length));

            foreach (Match match in ContractionRegex().Matches(text))
            {
                if (_isContraction(match.Groups[1].Value))
                    segments.Add((match.Index, match.Index + match.Length));
            }

            // Merge overlapping matches so each range is drawn only once; otherwise the dash patterns of two
            // overlapping lines combine and look solid.
            segments.Sort();
            var start = -1;
            var end = -1;
            foreach (var segment in segments)
            {
                if (start >= 0 && segment.Start < end)
                {
                    end = Math.Max(end, segment.End);
                    continue;
                }

                if (start >= 0)
                    DrawUnderline(textView, drawingContext, line.Offset + start, end - start);

                start = segment.Start;
                end = segment.End;
            }

            if (start >= 0)
                DrawUnderline(textView, drawingContext, line.Offset + start, end - start);
        }
    }

    [GeneratedRegex(@"@?(\+?[\w]+(?:_[\w]+)*)")]
    private static partial Regex ContractionRegex();

    private static void DrawUnderline(TextView textView, DrawingContext drawingContext, int offset, int length)
    {
        var document = textView.Document;
        if (document == null)
            return;

        // Measure each word separately so that trailing whitespace at a wrap point is never included, then join
        // words that share a visual line so the underline is continuous across the spaces between them.
        var text = document.GetText(offset, length);
        double? startX = null;
        double endX = 0;
        double y = 0;

        foreach (Match word in WordRegex().Matches(text))
        {
            var left = GetPoint(textView, offset + word.Index);
            var right = GetPoint(textView, offset + word.Index + word.Length);

            if (startX != null && Math.Abs(left.Y - y) < 1)
            {
                endX = right.X;
                continue;
            }

            if (startX != null)
                Draw(drawingContext, startX.Value, endX, y);

            startX = left.X;
            endX = right.X;
            y = left.Y;
        }

        if (startX != null)
            Draw(drawingContext, startX.Value, endX, y);
    }

    private static Point GetPoint(TextView textView, int offset)
    {
        var location = textView.Document!.GetLocation(offset);
        return textView.GetVisualPosition(new TextViewPosition(location), VisualYPosition.LineBottom) -
               textView.ScrollOffset;
    }

    private static void Draw(DrawingContext drawingContext, double left, double right, double bottom)
    {
        var y = Math.Round(bottom - UnderlineGap) + 0.5;
        drawingContext.DrawLine(UnderlinePen, new Point(left, y), new Point(right, y));
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex WordRegex();
}
