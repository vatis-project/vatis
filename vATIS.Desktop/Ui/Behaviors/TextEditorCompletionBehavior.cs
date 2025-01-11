// <copyright file="TextEditorCompletionBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Utils;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Defines a behavior that adds code completion functionality to an associated <see cref="TextEditor"/> control.
/// </summary>
public class TextEditorCompletionBehavior : Behavior<TextEditor>
{
    /// <summary>
    /// Gets or sets the list of completion data entries used for populating the code completion suggestions in the associated <see cref="TextEditor"/> control.
    /// </summary>
    public static readonly StyledProperty<List<ICompletionData>> CompletionDataProperty =
        AvaloniaProperty.Register<TextEditorCompletionBehavior, List<ICompletionData>>(nameof(CompletionData));

    private CompletionWindow? _completionWindow;
    private TextEditor? _textEditor;

    /// <summary>
    /// Gets or sets the list of completion data entries used for populating the code completion suggestions in the associated <see cref="TextEditor"/> control.
    /// </summary>
    public List<ICompletionData> CompletionData
    {
        get => GetValue(CompletionDataProperty);
        set => SetValue(CompletionDataProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } editor)
        {
            throw new NullReferenceException("AssociatedObject is null");
        }

        _textEditor = editor;
        _textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
        _textEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        if (_completionWindow == null && !string.IsNullOrWhiteSpace(e.Text) && !char.IsNumber(e.Text[0]))
        {
            if (e.Text == "@")
            {
                if (CompletionData.Count > 0)
                {
                    ShowCompletion();
                }
            }
        }
        else if (_completionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
        {
            _completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void ShowCompletion()
    {
        if (_completionWindow != null)
        {
            return;
        }

        _completionWindow = new CompletionWindow(_textEditor?.TextArea);
        _completionWindow.CompletionList.CompletionData.AddRange(CompletionData);
        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }
}
