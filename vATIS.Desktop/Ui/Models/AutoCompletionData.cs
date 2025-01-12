// <copyright file="AutoCompletionData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace Vatsim.Vatis.Ui.Models;

/// <summary>
/// Represents an entry for auto-completion suggestions.
/// Implements the <see cref="ICompletionData"/> interface to provide completion data for a code editor or similar component.
/// </summary>
public class AutoCompletionData : ICompletionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoCompletionData"/> class.
    /// </summary>
    /// <param name="text">The text associated with the completion data.</param>
    /// <param name="description">The description providing additional information about the completion data.</param>
    public AutoCompletionData(string text, string description)
    {
        Text = text;
        Description = description;
    }

    /// <inheritdoc/>
    public IImage? Image => null;

    /// <inheritdoc/>
    public string Text { get; }

    /// <inheritdoc/>
    public object Content => new TextBlock { Text = Text };

    /// <inheritdoc/>
    public object Description { get; }

    /// <inheritdoc/>
    public double Priority => 0;

    /// <inheritdoc/>
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
