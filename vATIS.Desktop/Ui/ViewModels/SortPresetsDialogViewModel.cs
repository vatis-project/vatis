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
    private ObservableCollection<AtisPreset> _presets = [];

    public SortPresetsDialogViewModel()
    {
        var textColumnLength = new GridLength(1, GridUnitType.Star);
        this.Source = new FlatTreeDataGridSource<AtisPreset>(this.Presets)
        {
            Columns =
            {
                new TextColumn<AtisPreset, string>(
                    "",
                    x => x.ToString(),
                    textColumnLength,
                    new TextColumnOptions<AtisPreset>
                    {
                        TextWrapping = TextWrapping.Wrap
                    })
            }
        };
        this.Source.RowSelection!.SingleSelect = false;

        var canMoveUp = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x < this.Presets.Count - 1);

        this.CloseWindowCommand = ReactiveCommand.Create<ICloseable>(HandleCloseWindow);
        this.MovePresetUpCommand = ReactiveCommand.Create(this.HandleMovePresetUp, canMoveUp);
        this.MovePresetDownCommand = ReactiveCommand.Create(this.HandleMovePresetDown, canMoveDown);
    }

    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    public ReactiveCommand<Unit, Unit> MovePresetUpCommand { get; }

    public ReactiveCommand<Unit, Unit> MovePresetDownCommand { get; }

    public ObservableCollection<AtisPreset> Presets
    {
        get => this._presets;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presets, value);
            this.Source.Items = this._presets.OrderBy(x => x.Ordinal);
        }
    }

    public FlatTreeDataGridSource<AtisPreset> Source { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.CloseWindowCommand.Dispose();
        this.MovePresetUpCommand.Dispose();
        this.MovePresetDownCommand.Dispose();
    }

    private void HandleMovePresetUp()
    {
        if (this.Source.RowSelection?.SelectedIndex >= 1)
        {
            var definition = this.Source.RowSelection.SelectedItem;
            var newIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault() - 1;
            var oldIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault();
            if (definition != null)
            {
                this.Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                this.Source.Items = this.Presets.OrderBy(x => x.Ordinal).ToList();
                this.Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMovePresetDown()
    {
        if (this.Source.RowSelection?.SelectedIndex <= this.Source.Items.Count() - 1)
        {
            var definition = this.Source.RowSelection.SelectedItem;
            var oldIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault();
            var newIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault() + 1;
            if (definition != null)
            {
                this.Presets.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                this.Source.Items = this.Presets.OrderBy(x => x.Ordinal).ToList();
                this.Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private static void HandleCloseWindow(ICloseable window)
    {
        window.Close();
    }
}