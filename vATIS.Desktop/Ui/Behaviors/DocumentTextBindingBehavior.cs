// <copyright file="DocumentTextBindingBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Behavior that binds the text content of a <see cref="TextEditor"/> to a dependency property.
/// </summary>
public class DocumentTextBindingBehavior : Behavior<TextEditor>
{
    /// <summary>
    /// Defines the Text dependency property for binding the text content.
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<DocumentTextBindingBehavior, string>(nameof(Text));

    private TextEditor? _textEditor;

    /// <summary>
    /// Gets or sets the text content of the associated <see cref="TextEditor"/>.
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } textEditor)
        {
            _textEditor = textEditor;
            _textEditor.TextChanged += OnTextChanged;
            this.GetObservable(TextProperty).Subscribe(TextPropertyChanged);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (_textEditor != null)
        {
            _textEditor.TextChanged -= OnTextChanged;
        }
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_textEditor is { Document: not null })
        {
            Text = _textEditor.Document.Text;
        }
    }

    private void TextPropertyChanged(string? text)
    {
        if (_textEditor is { Document: not null })
        {
            text ??= string.Empty;

            var caretOffset = _textEditor.CaretOffset;

            if (_textEditor.Document.Text != text)
            {
                _textEditor.Document.Text = text;
                _textEditor.CaretOffset = Math.Min(caretOffset, text.Length);
            }
        }
    }
}
