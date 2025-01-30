// <copyright file="ReadOnlySegmentTransformer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// A transformer that colorizes read-only segments of a document line.
/// </summary>
public class ReadOnlySegmentTransformer : DocumentColorizingTransformer
{
    private readonly TextSegmentCollection<TextSegment> _readOnlySegments;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlySegmentTransformer"/> class.
    /// </summary>
    /// <param name="readOnlySegments">The collection of read-only segments to colorize.</param>
    public ReadOnlySegmentTransformer(TextSegmentCollection<TextSegment> readOnlySegments)
    {
        _readOnlySegments = readOnlySegments;
    }

    /// <summary>
    /// Colorizes the specified line by applying color to the overlapping read-only segments.
    /// </summary>
    /// <param name="line">The document line to colorize.</param>
    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var segment in _readOnlySegments.FindOverlappingSegments(line.Offset, line.Length))
        {
            ChangeLinePart(
                Math.Max(segment.StartOffset, line.Offset),
                Math.Min(segment.EndOffset, line.Offset + line.Length),
                element =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.Aqua);
                }
            );
        }
    }
}
