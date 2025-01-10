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

    private CompletionWindow? completionWindow;
    private TextEditor? textEditor;

    /// <summary>
    /// Gets or sets the list of completion data entries used for populating the code completion suggestions in the associated <see cref="TextEditor"/> control.
    /// </summary>
    public List<ICompletionData> CompletionData
    {
        get => this.GetValue(CompletionDataProperty);
        set => this.SetValue(CompletionDataProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.AssociatedObject is not { } editor)
        {
            throw new NullReferenceException("AssociatedObject is null");
        }

        this.textEditor = editor;
        this.textEditor.TextArea.TextEntered += this.TextAreaOnTextEntered;
        this.textEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        if (this.completionWindow == null && !string.IsNullOrWhiteSpace(e.Text) && !char.IsNumber(e.Text[0]))
        {
            if (e.Text == "@")
            {
                if (this.CompletionData.Count > 0)
                {
                    this.ShowCompletion();
                }
            }
        }
        else if (this.completionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
        {
            this.completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void ShowCompletion()
    {
        if (this.completionWindow != null)
        {
            return;
        }

        this.completionWindow = new CompletionWindow(this.textEditor?.TextArea);
        this.completionWindow.CompletionList.CompletionData.AddRange(this.CompletionData);
        this.completionWindow.Show();
        this.completionWindow.Closed += (_, _) => this.completionWindow = null;
    }
}
