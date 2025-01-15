// <copyright file="ContractionsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides a view model for managing contractions within the ATIS configuration.
/// </summary>
public class ContractionsViewModel : ReactiveViewModelBase
{
    private readonly IAppConfig _appConfig;
    private readonly IWindowFactory _windowFactory;
    private IDialogOwner? _dialogOwner;
    private AtisStation? _selectedStation;
    private bool _hasUnsavedChanges;
    private ObservableCollection<ContractionMeta>? _contractions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractionsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory used to create windows.</param>
    /// <param name="appConfig">The application configuration.</param>
    public ContractionsViewModel(IWindowFactory windowFactory, IAppConfig appConfig)
    {
        _windowFactory = windowFactory;
        _appConfig = appConfig;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(HandleCellEditEnding);
        NewContractionCommand = ReactiveCommand.CreateFromTask(HandleNewContraction);
        DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(HandleDeleteContraction);
    }

    /// <summary>
    /// Gets the current contractions associated with the view model.
    /// </summary>
    public List<Tuple<int, ContractionMeta>> CurrentContractions { get; private set; } = [];

    /// <summary>
    /// Gets the command to handle changes to the ATIS station.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets the command executed when a cell edit operation is ending in the data grid.
    /// </summary>
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }

    /// <summary>
    /// Gets the command used to add a new contraction.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewContractionCommand { get; }

    /// <summary>
    /// Gets the command to delete a selected contraction.
    /// </summary>
    public ReactiveCommand<ContractionMeta, Unit> DeleteContractionCommand { get; }

    /// <summary>
    /// Gets or sets the currently selected ATIS station associated with the view model.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes in the view model.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the collection of contractions associated with the ATIS configuration.
    /// </summary>
    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => _contractions;
        set => this.RaiseAndSetIfChanged(ref _contractions, value);
    }

    /// <summary>
    /// Sets the dialog owner for the current instance.
    /// </summary>
    /// <param name="owner">The <see cref="IDialogOwner"/> to associate with this instance.</param>
    public void SetDialogOwner(IDialogOwner? owner)
    {
        _dialogOwner = owner;
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        SelectedStation = station;
        CurrentContractions = [];
        Contractions = new ObservableCollection<ContractionMeta>(station.Contractions);
    }

    private async Task HandleDeleteContraction(ContractionMeta? item)
    {
        if (item == null || Contractions == null || _dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)_dialogOwner,
                "Are you sure you want to delete the selected contraction?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (Contractions.Remove(item))
            {
                SelectedStation.Contractions.Remove(item);
                _appConfig.SaveConfig();
                MessageBus.Current.SendMessage(new ContractionsUpdated(SelectedStation.Id));
            }
        }
    }

    private async Task HandleNewContraction()
    {
        if (_dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousVariableValue = string.Empty;
            var previousTextValue = string.Empty;
            var previousSpokenValue = string.Empty;

            var dialog = _windowFactory.CreateNewContractionDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is NewContractionDialogViewModel context)
            {
                context.Variable = previousVariableValue;
                context.Text = previousTextValue;
                context.Spoken = previousSpokenValue;

                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        previousVariableValue = context.Variable;
                        previousTextValue = context.Text;
                        previousSpokenValue = context.Spoken;

                        context.ClearAllErrors();

                        if (string.IsNullOrEmpty(context.Variable))
                        {
                            context.RaiseError("Variable", "Value is required");
                        }

                        if (string.IsNullOrWhiteSpace(context.Text))
                        {
                            context.RaiseError("Text", "Value is required.");
                        }

                        if (string.IsNullOrWhiteSpace(context.Spoken))
                        {
                            context.RaiseError("Spoken", "Value is required.");
                        }

                        if (SelectedStation.Contractions.Any(x => x.VariableName == context.Variable))
                        {
                            context.RaiseError("Variable", "A contraction with this variable name already exist.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        SelectedStation.Contractions.Add(
                            new ContractionMeta
                            {
                                VariableName = context.Variable,
                                Text = context.Text,
                                Voice = context.Spoken,
                            });
                        _appConfig.SaveConfig();

                        Contractions = [];
                        foreach (var item in SelectedStation.Contractions)
                        {
                            Contractions.Add(item);
                        }

                        MessageBus.Current.SendMessage(new ContractionsUpdated(SelectedStation.Id));
                    }
                };
                await dialog.ShowDialog((Window)_dialogOwner);
            }
        }
    }

    private void HandleCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            HasUnsavedChanges = true;
        }
    }
}
