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

public class AutoCompletionData : ICompletionData
{
    public IImage? Image => null;
    public string Text { get; }
    public object Content => new TextBlock { Text = Text };
    public object Description { get; }
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }

    public AutoCompletionData(string text, string description)
    {
        Text = text;
        Description = description;
    }
}
