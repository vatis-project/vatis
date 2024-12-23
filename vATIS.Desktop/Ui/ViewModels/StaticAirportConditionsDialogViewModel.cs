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

public class StaticAirportConditionsDialogViewModel : ReactiveViewModelBase
{
    private readonly IWindowFactory mWindowFactory;
    
    public Window? Owner { get; set; }
    
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> NewDefinitionCommand { get; }
    public ReactiveCommand<Unit, Unit> EditDefinitionCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteDefinitionCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveDefinitionUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveDefinitionDownCommand { get; }
    
    private bool mShowOverlay;
    public bool ShowOverlay
    {
        get => mShowOverlay;
        set => this.RaiseAndSetIfChanged(ref mShowOverlay, value);
    }

    private bool mHasDefinitions;
    public bool HasDefinitions
    {
        get => mHasDefinitions;
        set => this.RaiseAndSetIfChanged(ref mHasDefinitions, value);
    }

    private bool mIncludeBeforeFreeText;
    public bool IncludeBeforeFreeText
    {
        get => mIncludeBeforeFreeText;
        set => this.RaiseAndSetIfChanged(ref mIncludeBeforeFreeText, value);
    }
    
    private List<ICompletionData> mContractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => mContractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref mContractionCompletionData, value);
    }

    private ObservableCollection<StaticDefinition> mDefinitions = [];
    public ObservableCollection<StaticDefinition> Definitions
    {
        get => mDefinitions;
        set
        {
            this.RaiseAndSetIfChanged(ref mDefinitions, value);
            Source.Items = mDefinitions.OrderBy(x => x.Ordinal);
            HasDefinitions = mDefinitions.Count != 0;
        }
    }
    
    public FlatTreeDataGridSource<StaticDefinition> Source { get; }

    public StaticAirportConditionsDialogViewModel(IWindowFactory windowFactory)
    {
        mWindowFactory = windowFactory;

        var textColumnLength = new GridLength(1, GridUnitType.Star);
        var enabledColumn = new CheckBoxColumn<StaticDefinition>("", x => x.Enabled, (r, v) =>
        {
            r.Enabled = v;
        });
        var descriptionColumn = new TextColumn<StaticDefinition, string>("", x => x.ToString()!.ToUpperInvariant(), width: textColumnLength, new TextColumnOptions<StaticDefinition>()
        {
            TextWrapping = TextWrapping.Wrap
        });
        
        Source = new FlatTreeDataGridSource<StaticDefinition>(Definitions)
        {
            Columns =
            {
                enabledColumn,
                descriptionColumn
            }
        };
        Source.RowSelection!.SingleSelect = false;

        var canExecute = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedItem).Select(x => x != null);
        
        NewDefinitionCommand = ReactiveCommand.CreateFromTask(HandleNewDefinition);
        DeleteDefinitionCommand = ReactiveCommand.CreateFromTask(HandleDeleteDefinition, canExecute);
        EditDefinitionCommand = ReactiveCommand.CreateFromTask(HandleEditDefinition, canExecute);
        CloseWindowCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());

        var canMoveUp = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x > 0);
        var canMoveDown = this.WhenAnyValue(x => x.Source.RowSelection!.SelectedIndex)
            .Select(x => x < Definitions.Count - 1);

        MoveDefinitionUpCommand = ReactiveCommand.Create(HandleMoveDefinitionUp, canMoveUp);
        MoveDefinitionDownCommand = ReactiveCommand.Create(HandleMoveDefinitionDown, canMoveDown);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                ShowOverlay = lifetime.Windows.Count(w =>
                    w.GetType() != typeof(MainWindow) && 
                    w.GetType() != typeof(AtisConfigurationWindow)) > 1;
            };
        }
    }

    private void HandleMoveDefinitionUp()
    {
        if (Source.RowSelection?.SelectedIndex >= 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() - 1;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            if (definition != null)
            {
                Definitions.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Definitions.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void HandleMoveDefinitionDown()
    {
        if (Source.RowSelection?.SelectedIndex <= Source.Items.Count() - 1)
        {
            var definition = Source.RowSelection.SelectedItem;
            var oldIndex = Source.RowSelection.SelectedIndex.FirstOrDefault();
            var newIndex = Source.RowSelection.SelectedIndex.FirstOrDefault() + 1;
            if (definition != null)
            {
                Definitions.Move(oldIndex, newIndex);
                definition.Ordinal = newIndex;
                Source.Items = Definitions.OrderBy(x => x.Ordinal).ToList();
                Source.RowSelection.SelectedIndex = newIndex;
            }
        }
    }

    private void RemoveSelected()
    {
        var selectedItem = Source.RowSelection?.SelectedItem;
        if (selectedItem != null)
        {
            Definitions.Remove(selectedItem);
        }
        Source.Items = Definitions.OrderBy(x => x.Ordinal).ToList();
        HasDefinitions = Definitions.Count != 0;
    }

    private async Task HandleEditDefinition()
    {
        if (Owner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;
            
            if (Source.RowSelection?.SelectedItem is { } definition)
            {

                var dlg = mWindowFactory.CreateStaticDefinitionEditorDialog();
                dlg.Topmost = lifetime.MainWindow.Topmost;
                if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
                {
                    vm.Title = "Edit Airport Condition";
                    vm.DefinitionText = definition.Text.ToUpperInvariant();
                    vm.ContractionCompletionData = ContractionCompletionData;
                    vm.DialogResultChanged += (_, result) =>
                    {
                        if (result == DialogResult.Ok)
                        {
                            vm.ClearAllErrors();

                            if (Definitions.Any(x => x != definition && string.Equals(x.Text,
                                    vm.TextDocument?.Text, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                vm.RaiseError("DataValidation", "NOTAM already exists.");
                            }

                            if (string.IsNullOrWhiteSpace(vm.TextDocument?.Text))
                            {
                                vm.RaiseError("DataValidation", "Text is required.");
                            }

                            if (vm.HasErrors)
                                return;

                            var currentIndex = Source.RowSelection?.SelectedIndex.FirstOrDefault() ?? 0;
                            var text = vm.TextDocument?.Text.ToUpperInvariant() ?? "";

                            Definitions.Remove(definition);
                            Definitions.Insert(currentIndex, new StaticDefinition(text, currentIndex, definition.Enabled));
                            Source.Items = Definitions.OrderBy(x => x.Ordinal).ToList();
                        }
                    };
                    await dlg.ShowDialog(Owner);
                }
            }
        }
    }

    private async Task HandleNewDefinition()
    {
        if (Owner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;
            
            var dlg = mWindowFactory.CreateStaticDefinitionEditorDialog();
            dlg.Topmost = lifetime.MainWindow.Topmost;
            if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
            {
                vm.Title = "New Airport Condition";
                vm.ContractionCompletionData = ContractionCompletionData;
                vm.DialogResultChanged += (_, result) =>
                {
                    if (result == DialogResult.Ok)
                    {
                        vm.ClearAllErrors();

                        if (Definitions.Any(x =>
                                string.Equals(x.Text, vm.DefinitionText, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            vm.RaiseError("DataValidation", "This NOTAM already exists.");
                        }

                        if (string.IsNullOrWhiteSpace(vm.DefinitionText))
                        {
                            vm.RaiseError("DataValidation", "Text is required.");
                        }

                        if (vm.HasErrors)
                            return;
                    
                        var text = vm.TextDocument?.Text.ToUpperInvariant() ?? "";
                    
                        Definitions.Add(new StaticDefinition(text, Definitions.Count + 1));
                        Source.Items = Definitions.OrderBy(x => x.Ordinal).ToList();
                        HasDefinitions = Definitions.Count != 0;
                    }
                };
                await dlg.ShowDialog(Owner);
            }
        }
    }

    private async Task HandleDeleteDefinition()
    {
        if (Owner == null)
            return;

        if (await MessageBox.ShowDialog(Owner,
                "Are you sure you want to delete the selected airport condition?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            RemoveSelected();
        }
        Source.RowSelection?.Clear();
    }
}