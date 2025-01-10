using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace Vatsim.Vatis.Ui.Models;

public class AutoCompletionData : ICompletionData
{
    public AutoCompletionData(string text, string description)
    {
        this.Text = text;
        this.Description = description;
    }

    public IImage? Image => null;

    public string Text { get; }

    public object Content => new TextBlock { Text = this.Text };

    public object Description { get; }

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, this.Text);
    }
}