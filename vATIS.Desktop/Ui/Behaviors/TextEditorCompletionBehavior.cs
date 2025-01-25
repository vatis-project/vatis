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
    private int _triggerPosition = -1;

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
        _textEditor.TextArea.KeyUp += OnTextEditorKeyUp;
        _textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
        _textEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
    }

    private void OnTextEditorKeyUp(object? sender, KeyEventArgs e)
    {
        // If enter or tab is pressed when the completion window is open,
        // request insertion of the selection contraction.
        if ((e.Key == Key.Enter || e.Key == Key.Tab) && _completionWindow is not null)
        {
            _completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text))
            return;

        if (e.Text == "@")
        {
            // Record the position of the '@' symbol
            _triggerPosition = _textEditor?.TextArea.Caret.Offset - 1 ?? -1;
            TriggerCompletionWindow();
        }
    }

    private void TriggerCompletionWindow()
    {
        // Don't show completion window if there is no completion data.
        if (CompletionData.Count == 0)
            return;

        _completionWindow = null;
        _completionWindow ??= new CompletionWindow(_textEditor?.TextArea);
        _completionWindow.CompletionList.CompletionData.AddRange(CompletionData);
        _completionWindow.CompletionList.InsertionRequested += (_, _) => RemoveAtSymbol();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
        _completionWindow.Show();
    }

    private void RemoveAtSymbol()
    {
        if (_textEditor == null || _triggerPosition < 0)
            return;

        // Remove the '@' symbol if it's still present at the recorded position
        if (_triggerPosition < _textEditor.Document.TextLength &&
            _textEditor.Document.GetCharAt(_triggerPosition) == '@')
        {
            _textEditor.Document.Remove(_triggerPosition, 1);
        }

        // Reset the trigger position
        _triggerPosition = -1;
    }
}
