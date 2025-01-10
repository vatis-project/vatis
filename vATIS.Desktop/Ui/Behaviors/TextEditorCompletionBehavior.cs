using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Utils;

namespace Vatsim.Vatis.Ui.Behaviors;

public class TextEditorCompletionBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<List<ICompletionData>> CompletionDataProperty =
        AvaloniaProperty.Register<TextEditorCompletionBehavior, List<ICompletionData>>(nameof(CompletionData));

    private CompletionWindow? _completionWindow;
    private TextEditor _textEditor = null!;

    public List<ICompletionData> CompletionData
    {
        get => this.GetValue(CompletionDataProperty);
        set => this.SetValue(CompletionDataProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.AssociatedObject is not { } editor)
        {
            throw new NullReferenceException("AssociatedObject is null");
        }

        this._textEditor = editor;
        this._textEditor.TextArea.TextEntered += this.TextAreaOnTextEntered;
        this._textEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        if (this._completionWindow == null && !string.IsNullOrWhiteSpace(e.Text) && !char.IsNumber(e.Text[0]))
        {
            if (e.Text == "@")
            {
                if (this.CompletionData.Count > 0)
                {
                    this.ShowCompletion();
                }
            }
        }
        else if (this._completionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
        {
            this._completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void ShowCompletion()
    {
        if (this._completionWindow != null)
        {
            return;
        }

        this._completionWindow = new CompletionWindow(this._textEditor.TextArea);
        this._completionWindow.CompletionList.CompletionData.AddRange(this.CompletionData);
        this._completionWindow.Show();
        this._completionWindow.Closed += (_, _) => this._completionWindow = null;
    }
}