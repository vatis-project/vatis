// <copyright file="SortAtisStationsDialogViewModel.cs" company="Justin Shannon">
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
/// Represents the view model for the dialog used to sort ATIS stations.
/// </summary>
public class SortAtisStationsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private ObservableCollection<AtisStation> _stations = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SortAtisStationsDialogViewModel"/> class.
    /// </summary>
    public SortAtisStationsDialogViewModel()
    {
        var textColumnLength = new GridLength(1, GridUnitType.Star);
        Source = new FlatTreeDataGridSource<AtisStation>(AtisStations)
        {
            Columns =
            {
                new TextColumn<AtisStation, string>("", x => x.ToString(), width: textColumnLength,
                    new TextColumnOptions<AtisStation> { TextWrapping = TextWrapping.Wrap })
            }
        };
        Source.RowSelection!.SingleSelect = false;

        var canMoveUp = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x < AtisStations.Count - 1);

        CloseWindowCommand = ReactiveCommand.Create<ICloseable>(HandleCloseWindow);
        MoveStationUpCommand = ReactiveCommand.Create(HandleMoveStationUp, canMoveUp);
        MoveStationDownCommand = ReactiveCommand.Create(HandleMoveStationDown, canMoveDown);
    }

    /// <summary>
    /// Gets the command that closes the current window or dialog.
    /// </summary>
    /// <value>
    /// A reactive command that takes an <see cref="ICloseable"/> instance as a parameter and performs the close operation.
    /// </value>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected station up in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveStationUpCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected station down in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveStationDownCommand { get; }

    /// <summary>
    /// Gets or sets the collection of ATIS stations managed within the view model.
    /// </summary>
    public ObservableCollection<AtisStation> AtisStations
    {
        get => _stations;
        set
        {
            this.RaiseAndSetIfChanged(ref _stations, value);
            Source.Items = _stations.OrderBy(x => x.Ordinal);
        }
    }

    /// <summary>
    /// Gets the data source for the tree data grid used for sorting ATIS stations.
    /// </summary>
    public FlatTreeDataGridSource<AtisStation> Source { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        CloseWindowCommand.Dispose();
        MoveStationUpCommand.Dispose();
        MoveStationDownCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleCloseWindow(ICloseable window)
    {
        window.Close();
    }

    private void HandleMoveStationUp()
    {
        if (Source.RowSelection?.SelectedIndex >= 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() - 1;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            if (definition != null)
            {
                AtisStations.Move(oldIndex, newIndex);
                NormalizeOrdinals();
                Source.Items = AtisStations.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMoveStationDown()
    {
        if (Source.RowSelection?.SelectedIndex < Source.Items.Count() - 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() + 1;
            if (definition != null)
            {
                AtisStations.Move(oldIndex, newIndex);
                NormalizeOrdinals();
                Source.Items = AtisStations.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void NormalizeOrdinals()
    {
        for (var i = 0; i < AtisStations.Count; i++)
        {
            AtisStations[i].Ordinal = i;
        }
    }
}
