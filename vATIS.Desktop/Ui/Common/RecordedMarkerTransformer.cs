// <copyright file="RecordedMarkerTransformer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// Highlights the [RECORDED] marker in the voice ATIS text, where a recorded section is played.
/// </summary>
public class RecordedMarkerTransformer : DocumentColorizingTransformer
{
    private const string Marker = "[RECORDED]";
    private static readonly IBrush MarkerBrush = new SolidColorBrush(Color.Parse("#F5A623"));

    /// <summary>
    /// Colorizes each [RECORDED] marker on the line.
    /// </summary>
    /// <param name="line">The document line to colorize.</param>
    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        var index = text.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var start = line.Offset + index;
            ChangeLinePart(start, start + Marker.Length, element =>
            {
                element.TextRunProperties.SetForegroundBrush(MarkerBrush);
                element.TextRunProperties.SetTypeface(new Typeface(element.TextRunProperties.Typeface.FontFamily,
                    FontStyle.Normal, FontWeight.Bold));
            });

            index = text.IndexOf(Marker, index + Marker.Length, StringComparison.OrdinalIgnoreCase);
        }
    }
}
