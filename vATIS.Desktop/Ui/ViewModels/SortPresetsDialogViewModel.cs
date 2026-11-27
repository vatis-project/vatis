// <copyright file="SortPresetsDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Models;

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
        var canMoveUp = this.WhenAnyValue(x => x.Source.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.SelectedIndex)
            .Select(x => x >= 0 && x < Presets.Count - 1);

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
            Source.Items = _presets.OrderBy(x => x.Ordinal).ToList();
        }
    }

    /// <summary>
    /// Gets the rows shown in the list and the selected rows.
    /// </summary>
    public SelectableList<AtisPreset> Source { get; } = new();

    /// <inheritdoc />
    public void Dispose()
    {
        CloseWindowCommand.Dispose();
        MovePresetUpCommand.Dispose();
        MovePresetDownCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleCloseWindow(ICloseable window)
    {
        window.Close();
    }

    private void HandleMovePresetUp()
    {
        if (Source.SelectedIndex >= 1)
        {
            var definition = Source.SelectedItem;
            var newIndex = Source.SelectedIndex - 1;
            var oldIndex = Source.SelectedIndex;
            if (definition != null)
            {
                Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Presets.OrderBy(x => x.Ordinal).ToList();
                Source.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMovePresetDown()
    {
        if (Source.SelectedIndex >= 0 && Source.SelectedIndex < Source.Items.Count - 1)
        {
            var definition = Source.SelectedItem;
            var oldIndex = Source.SelectedIndex;
            var newIndex = Source.SelectedIndex + 1;
            if (definition != null)
            {
                Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Presets.OrderBy(x => x.Ordinal).ToList();
                Source.SelectedIndex = newIndex;
            }
        }
    }
}
