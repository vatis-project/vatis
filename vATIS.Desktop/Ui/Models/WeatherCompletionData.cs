// <copyright file="WeatherCompletionData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Vatsim.Vatis.Atis.Extensions;

namespace Vatsim.Vatis.Ui.Models;

/// <summary>
/// A suggestion in the weather condition editor.
/// </summary>
public class WeatherCompletionData : ICompletionData
{
    private readonly WeatherSuggestion _suggestion;
    private readonly Action<WeatherSuggestion> _inserted;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherCompletionData"/> class.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <param name="inserted">Called after the suggestion has been inserted.</param>
    public WeatherCompletionData(WeatherSuggestion suggestion, Action<WeatherSuggestion> inserted)
    {
        _suggestion = suggestion;
        _inserted = inserted;
    }

    /// <inheritdoc/>
    public IImage? Image => null;

    /// <inheritdoc/>
    public string Text => _suggestion.Text;

    /// <inheritdoc/>
    public object Content => new TextBlock { Text = _suggestion.Text };

    /// <inheritdoc/>
    public object Description => _suggestion.Description;

    /// <inheritdoc/>
    public double Priority => 0;

    /// <inheritdoc/>
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        // Typing a character such as a space or '<' only closes the list. A suggestion is accepted with
        // Enter, Tab or a double click, so what the user types is never replaced behind their back.
        if (insertionRequestEventArgs is TextInputEventArgs)
        {
            return;
        }

        var offset = completionSegment.Offset;
        textArea.Document.Replace(completionSegment, _suggestion.InsertText);
        textArea.Caret.Offset = offset + _suggestion.InsertText.Length;
        _inserted(_suggestion);
    }
}
