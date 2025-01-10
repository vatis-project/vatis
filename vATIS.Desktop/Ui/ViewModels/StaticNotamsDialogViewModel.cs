using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui.ViewModels;

public class StaticNotamsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowFactory _windowFactory;

    private List<ICompletionData> _contractionCompletionData = [];

    private ObservableCollection<StaticDefinition> _definitions = [];

    private bool _hasDefinitions;

    private bool _includeBeforeFreeText;

    private bool _showOverlay;

    public StaticNotamsDialogViewModel(IWindowFactory windowFactory)
    {
        this._windowFactory = windowFactory;

        var textColumnLength = new GridLength(1, GridUnitType.Star);
        var enabledColumn = new CheckBoxColumn<StaticDefinition>("", x => x.Enabled, (r, v) => { r.Enabled = v; });
        var descriptionColumn = new TextColumn<StaticDefinition, string>(
            "",
            x => x.ToString()!.ToUpperInvariant(),
            textColumnLength,
            new TextColumnOptions<StaticDefinition>
            {
                TextWrapping = TextWrapping.Wrap
            });

        this.Source = new FlatTreeDataGridSource<StaticDefinition>(this.Definitions)
        {
            Columns =
            {
                enabledColumn,
                descriptionColumn
            }
        };
        this.Source.RowSelection!.SingleSelect = false;

        var canExecute = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedItem).Select(x => x != null);

        this.NewDefinitionCommand = ReactiveCommand.CreateFromTask(this.HandleNewDefinition);
        this.DeleteDefinitionCommand = ReactiveCommand.CreateFromTask(this.HandleDeleteDefinition, canExecute);
        this.EditDefinitionCommand = ReactiveCommand.CreateFromTask(this.HandleEditDefinition, canExecute);
        this.CloseWindowCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());

        var canMoveUp = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x < this.Definitions.Count - 1);

        this.MoveDefinitionUpCommand = ReactiveCommand.Create(this.HandleMoveDefinitionUp, canMoveUp);
        this.MoveDefinitionDownCommand = ReactiveCommand.Create(this.HandleMoveDefinitionDown, canMoveDown);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count(
                    w =>
                        w.GetType() != typeof(MainWindow) && w.GetType() != typeof(AtisConfigurationWindow)) > 1;
            };
        }
    }

    public Window? Owner { get; set; }

    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    public ReactiveCommand<Unit, Unit> NewDefinitionCommand { get; }

    public ReactiveCommand<Unit, Unit> EditDefinitionCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteDefinitionCommand { get; }

    public ReactiveCommand<Unit, Unit> MoveDefinitionUpCommand { get; }

    public ReactiveCommand<Unit, Unit> MoveDefinitionDownCommand { get; }

    public bool ShowOverlay
    {
        get => this._showOverlay;
        set => this.RaiseAndSetIfChanged(ref this._showOverlay, value);
    }

    public bool HasDefinitions
    {
        get => this._hasDefinitions;
        set => this.RaiseAndSetIfChanged(ref this._hasDefinitions, value);
    }

    public bool IncludeBeforeFreeText
    {
        get => this._includeBeforeFreeText;
        set => this.RaiseAndSetIfChanged(ref this._includeBeforeFreeText, value);
    }

    public List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
    }

    public ObservableCollection<StaticDefinition> Definitions
    {
        get => this._definitions;
        set
        {
            this.RaiseAndSetIfChanged(ref this._definitions, value);
            this.Source.Items = this._definitions.OrderBy(x => x.Ordinal);
            this.HasDefinitions = this._definitions.Count != 0;
        }
    }

    public FlatTreeDataGridSource<StaticDefinition> Source { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this.CloseWindowCommand.Dispose();
        this.NewDefinitionCommand.Dispose();
        this.DeleteDefinitionCommand.Dispose();
        this.EditDefinitionCommand.Dispose();
        this.MoveDefinitionDownCommand.Dispose();
        this.MoveDefinitionUpCommand.Dispose();
    }

    private void HandleMoveDefinitionUp()
    {
        if (this.Source.RowSelection?.SelectedIndex >= 1)
        {
            var definition = this.Source.RowSelection.SelectedItem;
            var newIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault() - 1;
            var oldIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault();
            if (definition != null)
            {
                this.Definitions.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                this.Source.Items = this.Definitions.OrderBy(x => x.Ordinal).ToList();
                this.Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMoveDefinitionDown()
    {
        if (this.Source.RowSelection?.SelectedIndex <= this.Source.Items.Count() - 1)
        {
            var definition = this.Source.RowSelection.SelectedItem;
            var oldIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault();
            var newIndex = this.Source.RowSelection.SelectedIndex.FirstOrDefault() + 1;
            if (definition != null)
            {
                this.Definitions.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                this.Source.Items = this.Definitions.OrderBy(x => x.Ordinal).ToList();
                this.Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void RemoveSelected()
    {
        var selectedItem = this.Source.RowSelection?.SelectedItem;
        if (selectedItem != null)
        {
            this.Definitions.Remove(selectedItem);
        }

        this.Source.Items = this.Definitions.OrderBy(x => x.Ordinal).ToList();
        this.HasDefinitions = this.Definitions.Count != 0;
    }

    private async Task HandleEditDefinition()
    {
        if (this.Owner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            if (this.Source.RowSelection?.SelectedItem is { } definition)
            {
                var dlg = this._windowFactory.CreateStaticDefinitionEditorDialog();
                dlg.Topmost = lifetime.MainWindow.Topmost;
                if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
                {
                    vm.Title = "Edit NOTAM";
                    vm.DefinitionText = definition.Text.ToUpperInvariant();
                    vm.ContractionCompletionData = this.ContractionCompletionData;
                    vm.DialogResultChanged += (_, result) =>
                    {
                        if (result == DialogResult.Ok)
                        {
                            vm.ClearAllErrors();

                            if (this.Definitions.Any(
                                    x => x != definition && string.Equals(
                                        x.Text,
                                        vm.TextDocument?.Text,
                                        StringComparison.InvariantCultureIgnoreCase)))
                            {
                                vm.RaiseError("DataValidation", "NOTAM already exists.");
                            }

                            if (string.IsNullOrWhiteSpace(vm.TextDocument?.Text))
                            {
                                vm.RaiseError("DataValidation", "Text is required.");
                            }

                            if (vm.HasErrors)
                            {
                                return;
                            }

                            var currentIndex = this.Source.RowSelection?.SelectedIndex.FirstOrDefault();

                            this.Definitions.Remove(definition);
                            this.Definitions.Insert(
                                currentIndex ?? 0,
                                new StaticDefinition(
                                    vm.TextDocument?.Text.ToUpperInvariant() ?? string.Empty,
                                    currentIndex ?? 0,
                                    definition.Enabled));
                            this.Source.Items = this.Definitions.OrderBy(x => x.Ordinal).ToList();
                        }
                    };
                    await dlg.ShowDialog(this.Owner);
                }
            }
        }
    }

    private async Task HandleNewDefinition()
    {
        if (this.Owner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var dlg = this._windowFactory.CreateStaticDefinitionEditorDialog();
            dlg.Topmost = lifetime.MainWindow.Topmost;
            if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
            {
                vm.Title = "New NOTAM";
                vm.ContractionCompletionData = this.ContractionCompletionData;
                vm.DialogResultChanged += (_, result) =>
                {
                    if (result == DialogResult.Ok)
                    {
                        vm.ClearAllErrors();

                        if (this.Definitions.Any(
                                x =>
                                    string.Equals(
                                        x.Text,
                                        vm.DefinitionText,
                                        StringComparison.InvariantCultureIgnoreCase)))
                        {
                            vm.RaiseError("DataValidation", "This NOTAM already exists.");
                        }

                        if (string.IsNullOrWhiteSpace(vm.DefinitionText))
                        {
                            vm.RaiseError("DataValidation", "Text is required.");
                        }

                        if (vm.HasErrors)
                        {
                            return;
                        }

                        this.Definitions.Add(
                            new StaticDefinition(
                                vm.DefinitionText?.Trim().ToUpperInvariant() ?? string.Empty,
                                this.Definitions.Count + 1));
                        this.Source.Items = this.Definitions.OrderBy(x => x.Ordinal).ToList();
                        this.HasDefinitions = this.Definitions.Count != 0;
                    }
                };
                await dlg.ShowDialog(this.Owner);
            }
        }
    }

    private async Task HandleDeleteDefinition()
    {
        if (this.Owner == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                this.Owner,
                "Are you sure you want to delete the selected NOTAM?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            this.RemoveSelected();
        }

        this.Source.RowSelection?.Clear();
    }
}