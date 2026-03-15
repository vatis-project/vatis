// <copyright file="DatisReplacementsView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents the view for displaying and managing D-ATIS text replacement rules.
/// </summary>
public partial class DatisReplacementsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatisReplacementsView"/> class.
    /// </summary>
    public DatisReplacementsView()
    {
        InitializeComponent();
    }

    private void ReplacementsGrid_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && DataContext is DatisReplacementsViewModel vm)
        {
            vm.CellEditEndingCommand.Execute().Subscribe();
        }
    }
}
