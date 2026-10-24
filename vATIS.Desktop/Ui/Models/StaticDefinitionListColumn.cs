// <copyright file="StaticDefinitionListColumn.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Models;

/// <summary>
/// Builds the message column of the Airport Conditions and NOTAMs lists: the message text, with the conditions it is
/// included under shown below it in muted text.
/// </summary>
public static class StaticDefinitionListColumn
{
    /// <summary>
    /// Creates the column.
    /// </summary>
    /// <param name="width">The column width.</param>
    /// <returns>The column.</returns>
    public static IColumn<StaticDefinition> Create(GridLength width)
    {
        return new TemplateColumn<StaticDefinition>(
            string.Empty,
            new FuncDataTemplate<StaticDefinition>((definition, _) => BuildCell(definition)),
            width: width);
    }

    private static Control BuildCell(StaticDefinition? definition)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new(4, 2) };
        if (definition == null)
        {
            return panel;
        }

        panel.Children.Add(new TextBlock
        {
            Text = definition.IsSpacer
                ? (definition.IsNewLine ? "(new line)" : "(blank line)")
                : definition.ToString().ToUpperInvariant(),
            TextWrapping = TextWrapping.Wrap,
            FontStyle = definition.IsSpacer ? FontStyle.Italic : FontStyle.Normal,
            Opacity = definition.IsSpacer ? 0.6 : 1,
        });

        foreach (var line in StaticDefinitionSummary.Describe(definition))
        {
            panel.Children.Add(new TextBlock
            {
                Text = line,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.6,
                FontStyle = FontStyle.Italic,
            });
        }

        return panel;
    }
}
