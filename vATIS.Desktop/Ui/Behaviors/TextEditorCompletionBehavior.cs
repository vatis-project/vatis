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
    private TextEditor mTextEditor = null!;
    private CompletionWindow? mCompletionWindow;

    public static readonly StyledProperty<List<ICompletionData>> CompletionDataProperty =
        AvaloniaProperty.Register<TextEditorCompletionBehavior, List<ICompletionData>>(nameof(CompletionData));

    public List<ICompletionData> CompletionData
    {
        get => GetValue(CompletionDataProperty);
        set => SetValue(CompletionDataProperty, value);
    }
    
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } editor)
        {
            throw new NullReferenceException("AssociatedObject is null");
        }
        
        mTextEditor = editor;
        mTextEditor.TextArea.TextEntered += TextAreaOnTextEntered;
        mTextEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        if (mCompletionWindow == null && !string.IsNullOrWhiteSpace(e.Text) && !char.IsNumber(e.Text[0]))
        {
            if (e.Text == "@")
            {
                if (CompletionData.Count > 0)
                {
                    ShowCompletion();
                }
            }
        }
        else if (mCompletionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
        {
            mCompletionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void ShowCompletion()
    {
        if (mCompletionWindow != null)
            return;
        
        mCompletionWindow = new CompletionWindow(mTextEditor.TextArea);
        mCompletionWindow.CompletionList.CompletionData.AddRange(CompletionData);
        mCompletionWindow.Show();
        mCompletionWindow.Closed += (_, _) => mCompletionWindow = null;
    }
}