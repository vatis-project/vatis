// <copyright file="ReleaseNoteText.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using ColorTextBlock.Avalonia;

namespace Vatsim.Vatis.Ui.Controls;

/// <summary>
/// Provides an attached property that fills a <see cref="CTextBlock"/> from text containing backticked code spans.
/// </summary>
public static class ReleaseNoteText
{
    /// <summary>
    /// Identifies the Text attached property.
    /// </summary>
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<CTextBlock, Control, string?>("Text");

    static ReleaseNoteText()
    {
        TextProperty.Changed.AddClassHandler<CTextBlock>((block, e) => Apply(block, e.NewValue as string));
    }

    /// <summary>
    /// Gets the text of the specified text block.
    /// </summary>
    /// <param name="element">The text block.</param>
    /// <returns>The text.</returns>
    public static string? GetText(CTextBlock element) => element.GetValue(TextProperty);

    /// <summary>
    /// Sets the text of the specified text block, rendering backticked spans as code.
    /// </summary>
    /// <param name="element">The text block.</param>
    /// <param name="value">The text.</param>
    public static void SetText(CTextBlock element, string? value) => element.SetValue(TextProperty, value);

    private static void Apply(CTextBlock block, string? text)
    {
        block.Content.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var parts = text.Split('`');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
                continue;

            // Odd segments sit between a pair of backticks. An unpaired trailing backtick stays plain text.
            if (i % 2 == 1 && i < parts.Length - 1)
                block.Content.Add(new CCode([new CRun { Text = parts[i] }]));
            else
                block.Content.Add(new CRun { Text = i % 2 == 1 ? "`" + parts[i] : parts[i] });
        }
    }
}
