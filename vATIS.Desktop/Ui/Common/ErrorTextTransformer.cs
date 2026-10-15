// <copyright file="ErrorTextTransformer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// A transformer that colorizes the error portion of a document whose text begins with "Error:".
/// Any trailing "Spoken text:" section is left in the default color.
/// </summary>
public class ErrorTextTransformer : DocumentColorizingTransformer
{
    private const string ErrorPrefix = "Error:";
    private const string SpokenTextMarker = "Spoken text:";
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6B6B"));

    /// <summary>
    /// Colorizes the portion of the line that belongs to the error message.
    /// </summary>
    /// <param name="line">The document line to colorize.</param>
    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.Text;
        if (!text.StartsWith(ErrorPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        var end = text.IndexOf(SpokenTextMarker, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            end = text.Length;

        var start = Math.Max(0, line.Offset);
        var finish = Math.Min(end, line.EndOffset);
        if (finish <= start)
            return;

        ChangeLinePart(start, finish, element => element.TextRunProperties.SetForegroundBrush(ErrorBrush));
    }
}
