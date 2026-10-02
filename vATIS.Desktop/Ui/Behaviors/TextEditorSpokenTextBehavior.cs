// <copyright file="TextEditorSpokenTextBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that underlines contraction variables and built-in speech tokens in a <see cref="TextEditor"/>
/// and shows their spoken form in a tooltip. The data comes from the editor's <see cref="ISpokenTextSource"/> data context.
/// </summary>
public class TextEditorSpokenTextBehavior : Behavior<TextEditor>
{
    private ContractionUnderlineRenderer? _renderer;
    private INotifyPropertyChanged? _source;

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null)
            return;

        _renderer = new ContractionUnderlineRenderer(IsContraction);
        AssociatedObject.TextArea.TextView.BackgroundRenderers.Add(_renderer);
        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerExited += OnPointerExited;
        AssociatedObject.DataContextChanged += OnDataContextChanged;
        HookDataContext();
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject == null)
            return;

        if (_renderer != null)
            AssociatedObject.TextArea.TextView.BackgroundRenderers.Remove(_renderer);

        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerExited -= OnPointerExited;
        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        UnhookDataContext();
    }

    private bool IsContraction(string word)
    {
        return AssociatedObject?.DataContext is ISpokenTextSource source &&
               (source.ContractionCompletionData.Any(x => x.Text == word) ||
                source.BuiltInContractions.ContainsKey(word));
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnhookDataContext();
        HookDataContext();
        AssociatedObject?.TextArea.TextView.Redraw();
    }

    private void HookDataContext()
    {
        if (AssociatedObject?.DataContext is ISpokenTextSource and INotifyPropertyChanged notify)
        {
            _source = notify;
            _source.PropertyChanged += OnSourcePropertyChanged;
        }
    }

    private void UnhookDataContext()
    {
        if (_source != null)
            _source.PropertyChanged -= OnSourcePropertyChanged;

        _source = null;
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISpokenTextSource.ContractionCompletionData))
            AssociatedObject?.TextArea.TextView.Redraw();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject != null)
            ToolTip.SetTip(AssociatedObject, null);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        string? tip = null;
        var position = AssociatedObject.GetPositionFromPoint(e.GetPosition(AssociatedObject));
        if (position != null && AssociatedObject.DataContext is ISpokenTextSource source)
        {
            var document = AssociatedObject.Document;
            var offset = document.GetOffset(position.Value.Location);
            var line = document.GetLineByOffset(offset);
            var lineText = document.GetText(line);

            bool Hovering(Match m) => offset >= line.Offset + m.Index && offset <= line.Offset + m.Index + m.Length;

            var token = ContractionUnderlineRenderer.BuiltInTokenRegex().Matches(lineText).FirstOrDefault(Hovering);
            if (token != null)
            {
                tip = source.GetSpokenText(token.Value);
            }
            else
            {
                var word = Regex.Matches(lineText, @"@?(\+?[\w]+(?:_[\w]+)*)").FirstOrDefault(Hovering);
                if (word != null)
                {
                    var name = word.Groups[1].Value;
                    tip = source.ContractionCompletionData.FirstOrDefault(x => x.Text == name)?.Description?.ToString() ??
                          (source.BuiltInContractions.TryGetValue(name, out var expansion) ? expansion : null);
                }
            }
        }

        ToolTip.SetTip(AssociatedObject, tip);
    }
}
