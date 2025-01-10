// <copyright file="ContractionsView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Avalonia.Controls;
using Slugify;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents the view for displaying and interacting with custom contractions.
/// </summary>
public partial class ContractionsView : UserControl
{
    private static readonly SlugHelper Slug = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractionsView"/> class.
    /// </summary>
    public ContractionsView()
    {
        this.InitializeComponent();
    }

    private void Contractions_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (this.DataContext is ContractionsViewModel vm)
        {
            if (e.Row.DataContext is ContractionMeta row)
            {
                vm.CurrentContractions.Add(new Tuple<int, ContractionMeta>(e.Row.Index, row));
            }
        }
    }

    private void Contractions_Validate(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (sender is not DataGrid dataGrid || e.EditAction != DataGridEditAction.Commit)
        {
            return;
        }

        if (this.DataContext is ContractionsViewModel vm)
        {
            if (e.EditingElement is TextBox textBox)
            {
                if (e.Column.Header.ToString() == "Variable")
                {
                    var slug = Slug.GenerateSlug(textBox.Text).Replace("-", "_").ToUpperInvariant();

                    if (vm.CurrentContractions.Any(
                            x => x.Item1 != e.Row.Index && string.Equals(
                                x.Item2.VariableName,
                                slug,
                                StringComparison.InvariantCultureIgnoreCase)))
                    {
                        e.Cancel = true;
                        dataGrid.CancelEdit();
                    }

                    textBox.Text = slug;
                }
            }
        }
    }
}
