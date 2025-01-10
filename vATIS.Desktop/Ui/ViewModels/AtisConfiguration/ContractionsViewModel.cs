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
    private readonly IAppConfig appConfig;
    private readonly IWindowFactory windowFactory;
    private IDialogOwner? dialogOwner;
    private AtisStation? selectedStation;
    private bool hasUnsavedChanges;
    private ObservableCollection<ContractionMeta>? contractions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractionsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory used to create windows.</param>
    /// <param name="appConfig">The application configuration.</param>
    public ContractionsViewModel(IWindowFactory windowFactory, IAppConfig appConfig)
    {
        this.windowFactory = windowFactory;
        this.appConfig = appConfig;

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleAtisStationChanged);
        this.CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(this.HandleCellEditEnding);
        this.NewContractionCommand = ReactiveCommand.CreateFromTask(this.HandleNewContraction);
        this.DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(this.HandleDeleteContraction);
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
        get => this.selectedStation;
        set => this.RaiseAndSetIfChanged(ref this.selectedStation, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes in the view model.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => this.hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the collection of contractions associated with the ATIS configuration.
    /// </summary>
    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => this.contractions;
        set => this.RaiseAndSetIfChanged(ref this.contractions, value);
    }

    /// <summary>
    /// Sets the dialog owner for the current instance.
    /// </summary>
    /// <param name="owner">The <see cref="IDialogOwner"/> to associate with this instance.</param>
    public void SetDialogOwner(IDialogOwner? owner)
    {
        this.dialogOwner = owner;
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedStation = station;
        this.CurrentContractions = [];
        this.Contractions = new ObservableCollection<ContractionMeta>(station.Contractions);
    }

    private async Task HandleDeleteContraction(ContractionMeta? item)
    {
        if (item == null || this.Contractions == null || this.dialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this.dialogOwner,
                "Are you sure you want to delete the selected contraction?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this.Contractions.Remove(item))
            {
                this.SelectedStation.Contractions.Remove(item);
                this.appConfig.SaveConfig();
                MessageBus.Current.SendMessage(new ContractionsUpdated(this.SelectedStation.Id));
            }
        }
    }

    private async Task HandleNewContraction()
    {
        if (this.dialogOwner == null || this.SelectedStation == null)
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

            var dialog = this.windowFactory.CreateNewContractionDialog();
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

                        if (this.SelectedStation.Contractions.Any(x => x.VariableName == context.Variable))
                        {
                            context.RaiseError("Variable", "A contraction with this variable name already exist.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        this.SelectedStation.Contractions.Add(
                            new ContractionMeta
                            {
                                VariableName = context.Variable,
                                Text = context.Text,
                                Voice = context.Spoken,
                            });
                        this.appConfig.SaveConfig();

                        this.Contractions = [];
                        foreach (var item in this.SelectedStation.Contractions)
                        {
                            this.Contractions.Add(item);
                        }

                        MessageBus.Current.SendMessage(new ContractionsUpdated(this.SelectedStation.Id));
                    }
                };
                await dialog.ShowDialog((Window)this.dialogOwner);
            }
        }
    }

    private void HandleCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            this.HasUnsavedChanges = true;
        }
    }
}
