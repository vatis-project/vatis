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

public class SortPresetsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> MovePresetUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MovePresetDownCommand { get; }

    private ObservableCollection<AtisPreset> _presets = [];
    public ObservableCollection<AtisPreset> Presets
    {
        get => _presets;
        set
        {
            this.RaiseAndSetIfChanged(ref _presets, value);
            Source.Items = _presets.OrderBy(x => x.Ordinal);
        }
    }

    public FlatTreeDataGridSource<AtisPreset> Source { get; }

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

    private static void HandleCloseWindow(ICloseable window)
    {
        window.Close();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseWindowCommand.Dispose();
        MovePresetUpCommand.Dispose();
        MovePresetDownCommand.Dispose();
    }
}
