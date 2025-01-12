// <copyright file="StaticNotamsDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the view model for the Static NOTAMs dialog, providing commands
/// and properties for managing static NOTAM definitions and related functionality.
/// </summary>
public class StaticNotamsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowFactory _windowFactory;
    private List<ICompletionData> _contractionCompletionData = [];
    private ObservableCollection<StaticDefinition> _definitions = [];
    private bool _showOverlay;
    private bool _hasDefinitions;
    private bool _includeBeforeFreeText;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticNotamsDialogViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">An instance of the factory used to create and manage application windows.</param>
    public StaticNotamsDialogViewModel(IWindowFactory windowFactory)
    {
        _windowFactory = windowFactory;

        var textColumnLength = new GridLength(1, GridUnitType.Star);
        var enabledColumn = new CheckBoxColumn<StaticDefinition>("", x => x.Enabled, (r, v) =>
        {
            r.Enabled = v;
        });
        var descriptionColumn = new TextColumn<StaticDefinition, string>("", x => x.ToString().ToUpperInvariant(),
            width: textColumnLength, new TextColumnOptions<StaticDefinition>()
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
                    w.GetType() != typeof(MainWindow) && w.GetType() != typeof(AtisConfigurationWindow)) > 1;
            };
        }
    }

        /// <summary>
    /// Gets or sets the window that serves as the owner of the current dialog.
    /// </summary>
    public Window? Owner { get; set; }

    /// <summary>
    /// Gets the command used to close a window.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command that creates a new definition.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewDefinitionCommand { get; }

    /// <summary>
    /// Gets the command used to handle editing an existing definition.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditDefinitionCommand { get; }

    /// <summary>
    /// Gets the command that deletes the selected definition from the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteDefinitionCommand { get; }

    /// <summary>
    /// Gets the command to move the selected definition up within the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDefinitionUpCommand { get; }

    /// <summary>
    /// Gets the command to move the selected definition down within the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDefinitionDownCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether an overlay is shown to obscure the background.
    /// </summary>
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether definitions are available.
    /// </summary>
    public bool HasDefinitions
    {
        get => _hasDefinitions;
        set => this.RaiseAndSetIfChanged(ref _hasDefinitions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the selected definitions
    /// should be included before the free-form NOTAMs text in the generated ATIS.
    /// </summary>
    public bool IncludeBeforeFreeText
    {
        get => _includeBeforeFreeText;
        set => this.RaiseAndSetIfChanged(ref _includeBeforeFreeText, value);
    }

    /// <summary>
    /// Gets or sets the list of completion data used for contraction suggestions in the NOTAM editor dialog.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the collection of static definitions used in the dialog.
    /// </summary>
    public ObservableCollection<StaticDefinition> Definitions
    {
        get => _definitions;
        set
        {
            this.RaiseAndSetIfChanged(ref _definitions, value);
            Source.Items = _definitions.OrderBy(x => x.Ordinal);
            HasDefinitions = _definitions.Count != 0;
        }
    }

    /// <summary>
    /// Gets the data source for the static definitions displayed in the grid.
    /// </summary>
    public FlatTreeDataGridSource<StaticDefinition> Source { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        CloseWindowCommand.Dispose();
        NewDefinitionCommand.Dispose();
        DeleteDefinitionCommand.Dispose();
        EditDefinitionCommand.Dispose();
        MoveDefinitionDownCommand.Dispose();
        MoveDefinitionUpCommand.Dispose();
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
                var dlg = _windowFactory.CreateStaticDefinitionEditorDialog();
                dlg.Topmost = lifetime.MainWindow.Topmost;
                if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
                {
                    vm.Title = "Edit NOTAM";
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

                            var currentIndex = Source.RowSelection?.SelectedIndex.FirstOrDefault();

                            Definitions.Remove(definition);
                            Definitions.Insert(currentIndex ?? 0,
                                new StaticDefinition(vm.TextDocument?.Text.ToUpperInvariant() ?? string.Empty, currentIndex ?? 0,
                                    definition.Enabled));
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

            var dlg = _windowFactory.CreateStaticDefinitionEditorDialog();
            dlg.Topmost = lifetime.MainWindow.Topmost;
            if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
            {
                vm.Title = "New NOTAM";
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

                        Definitions.Add(new StaticDefinition(vm.DefinitionText?.Trim().ToUpperInvariant() ?? string.Empty,
                            Definitions.Count + 1));
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
                "Are you sure you want to delete the selected NOTAM?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            RemoveSelected();
        }

        Source.RowSelection?.Clear();
    }
}
