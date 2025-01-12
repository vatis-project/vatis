// <copyright file="SortPresetsDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media;
using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the dialog used to manage and sort ATIS presets.
/// </summary>
public class SortPresetsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private ObservableCollection<AtisPreset> _presets = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SortPresetsDialogViewModel"/> class.
    /// </summary>
    public SortPresetsDialogViewModel()
    {
        var textColumnLength = new GridLength(1, GridUnitType.Star);
        Source = new FlatTreeDataGridSource<AtisPreset>(Presets)
        {
            Columns =
            {
                new TextColumn<AtisPreset, string>("", x => x.ToString(), width: textColumnLength,
                    new TextColumnOptions<AtisPreset>()
                    {
                        TextWrapping = TextWrapping.Wrap
                    })
            }
        };
        Source.RowSelection!.SingleSelect = false;

        var canMoveUp = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x < Presets.Count - 1);

        CloseWindowCommand = ReactiveCommand.Create<ICloseable>(HandleCloseWindow);
        MovePresetUpCommand = ReactiveCommand.Create(HandleMovePresetUp, canMoveUp);
        MovePresetDownCommand = ReactiveCommand.Create(HandleMovePresetDown, canMoveDown);
    }

    /// <summary>
    /// Gets the command that closes the current window or dialog.
    /// </summary>
    /// <value>
    /// A reactive command that takes an <see cref="ICloseable"/> instance as a parameter and performs the close operation.
    /// </value>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected ATIS preset up in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MovePresetUpCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected preset down in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MovePresetDownCommand { get; }

    /// <summary>
    /// Gets or sets the collection of ATIS presets managed within the view model.
    /// </summary>
    public ObservableCollection<AtisPreset> Presets
    {
        get => _presets;
        set
        {
            this.RaiseAndSetIfChanged(ref _presets, value);
            Source.Items = _presets.OrderBy(x => x.Ordinal);
        }
    }

    /// <summary>
    /// Gets the data source for the tree data grid used in managing and sorting ATIS presets.
    /// </summary>
    public FlatTreeDataGridSource<AtisPreset> Source { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseWindowCommand.Dispose();
        MovePresetUpCommand.Dispose();
        MovePresetDownCommand.Dispose();
    }

    private void HandleCloseWindow(ICloseable window)
    {
        window.Close();
    }

    private void HandleMovePresetUp()
    {
        if (Source.RowSelection?.SelectedIndex >= 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() - 1;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            if (definition != null)
            {
                Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Presets.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMovePresetDown()
    {
        if (Source.RowSelection?.SelectedIndex <= Source.Items.Count() - 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() + 1;
            if (definition != null)
            {
                Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Presets.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }
}
