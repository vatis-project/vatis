// <copyright file="StaticAirportConditionsDialogViewModel.cs" company="Justin Shannon">
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
/// Provides the ViewModel for the static airport conditions dialog, managing data, commands, and behaviors.
/// </summary>
public class StaticAirportConditionsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowFactory windowFactory;
    private List<ICompletionData> contractionCompletionData = [];
    private ObservableCollection<StaticDefinition> definitions = [];
    private bool hasDefinitions;
    private bool includeBeforeFreeText;
    private bool showOverlay;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticAirportConditionsDialogViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory used to create windows in the application.</param>
    public StaticAirportConditionsDialogViewModel(IWindowFactory windowFactory)
    {
        this.windowFactory = windowFactory;

        var textColumnLength = new GridLength(1, GridUnitType.Star);
        var enabledColumn = new CheckBoxColumn<StaticDefinition>(
            string.Empty,
            x => x.Enabled,
            (r, v) => { r.Enabled = v; });
        var descriptionColumn = new TextColumn<StaticDefinition, string>(
            string.Empty,
            x => x.ToString().ToUpperInvariant(),
            textColumnLength,
            new TextColumnOptions<StaticDefinition>
            {
                TextWrapping = TextWrapping.Wrap,
            });

        this.Source = new FlatTreeDataGridSource<StaticDefinition>(this.Definitions)
        {
            Columns =
            {
                enabledColumn,
                descriptionColumn,
            },
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
                        w.GetType() != typeof(MainWindow) &&
                        w.GetType() != typeof(AtisConfigurationWindow)) > 1;
            };
        }
    }

    /// <summary>
    /// Gets or sets the owner window associated with the dialog.
    /// </summary>
    public Window? Owner { get; set; }

    /// <summary>
    /// Gets the command to close a window implementing the <see cref="ICloseable"/> interface.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command used to create a new definition.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewDefinitionCommand { get; }

    /// <summary>
    /// Gets the command used to handle the editing of an existing definition.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditDefinitionCommand { get; }

    /// <summary>
    /// Gets the command for deleting a selected definition.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteDefinitionCommand { get; }

    /// <summary>
    /// Gets the command to move the selected definition up in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDefinitionUpCommand { get; }

    /// <summary>
    /// Gets the command that moves a definition down in the list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDefinitionDownCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the background overlay is visible.
    /// </summary>
    public bool ShowOverlay
    {
        get => this.showOverlay;
        set => this.RaiseAndSetIfChanged(ref this.showOverlay, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog has any definitions available.
    /// </summary>
    public bool HasDefinitions
    {
        get => this.hasDefinitions;
        set => this.RaiseAndSetIfChanged(ref this.hasDefinitions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the selected definitions
    /// will be included before the free-form airport conditions text in
    /// the generated ATIS.
    /// </summary>
    public bool IncludeBeforeFreeText
    {
        get => this.includeBeforeFreeText;
        set => this.RaiseAndSetIfChanged(ref this.includeBeforeFreeText, value);
    }

    /// <summary>
    /// Gets or sets the list of contraction completion data used for providing completion suggestions.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => this.contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this.contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the definitions associated with the airport conditions.
    /// </summary>
    public ObservableCollection<StaticDefinition> Definitions
    {
        get => this.definitions;
        set
        {
            this.RaiseAndSetIfChanged(ref this.definitions, value);
            this.Source.Items = this.definitions.OrderBy(x => x.Ordinal);
            this.HasDefinitions = this.definitions.Count != 0;
        }
    }

    /// <summary>
    /// Gets the data source for the FlatTreeDataGrid used to manage <see cref="StaticDefinition"/> entries.
    /// </summary>
    public FlatTreeDataGridSource<StaticDefinition> Source { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.CloseWindowCommand.Dispose();
        this.NewDefinitionCommand.Dispose();
        this.EditDefinitionCommand.Dispose();
        this.DeleteDefinitionCommand.Dispose();
        this.MoveDefinitionUpCommand.Dispose();
        this.MoveDefinitionDownCommand.Dispose();
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
                var dlg = this.windowFactory.CreateStaticDefinitionEditorDialog();
                dlg.Topmost = lifetime.MainWindow.Topmost;
                if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
                {
                    vm.Title = "Edit Airport Condition";
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

                            var currentIndex = this.Source.RowSelection?.SelectedIndex.FirstOrDefault() ?? 0;
                            var text = vm.TextDocument?.Text.ToUpperInvariant() ?? string.Empty;

                            this.Definitions.Remove(definition);
                            this.Definitions.Insert(
                                currentIndex,
                                new StaticDefinition(text, currentIndex, definition.Enabled));
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

            var dlg = this.windowFactory.CreateStaticDefinitionEditorDialog();
            dlg.Topmost = lifetime.MainWindow.Topmost;
            if (dlg.DataContext is StaticDefinitionEditorDialogViewModel vm)
            {
                vm.Title = "New Airport Condition";
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

                        var text = vm.TextDocument?.Text.ToUpperInvariant() ?? string.Empty;

                        this.Definitions.Add(new StaticDefinition(text, this.Definitions.Count + 1));
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
                "Are you sure you want to delete the selected airport condition?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            this.RemoveSelected();
        }

        this.Source.RowSelection?.Clear();
    }
}
