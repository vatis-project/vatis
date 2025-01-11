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

public class ContractionsViewModel : ReactiveViewModelBase
{
    private readonly IWindowFactory _windowFactory;
    private readonly IAppConfig _appConfig;
    private IDialogOwner? _dialogOwner;

    public List<Tuple<int, ContractionMeta>> CurrentContractions { get; private set; } = [];
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }
    public ReactiveCommand<Unit, Unit> NewContractionCommand { get; }
    public ReactiveCommand<ContractionMeta, Unit> DeleteContractionCommand { get; }

    #region Reactive Properties
    private AtisStation? _selectedStation;
    private AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    private ObservableCollection<ContractionMeta>? _contractions;
    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => _contractions;
        set => this.RaiseAndSetIfChanged(ref _contractions, value);
    }
    #endregion

    public ContractionsViewModel(IWindowFactory windowFactory, IAppConfig appConfig)
    {
        _windowFactory = windowFactory;
        _appConfig = appConfig;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(HandleCellEditEnding);
        NewContractionCommand = ReactiveCommand.CreateFromTask(HandleNewContraction);
        DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(HandleDeleteContraction);
    }

    public void SetDialogOwner(IDialogOwner? dialogOwner)
    {
        _dialogOwner = dialogOwner;
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
            return;

        SelectedStation = station;
        CurrentContractions = [];
        Contractions = new ObservableCollection<ContractionMeta>(station.Contractions);
    }

    private async Task HandleDeleteContraction(ContractionMeta? item)
    {
        if (item == null || Contractions == null || _dialogOwner == null || SelectedStation == null)
            return;

        if (await MessageBox.ShowDialog((Window)_dialogOwner,
                "Are you sure you want to delete the selected contraction?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
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
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousVariableValue = "";
            var previousTextValue = "";
            var previousSpokenValue = "";

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

                        if (context.HasErrors) return;

                        SelectedStation.Contractions.Add(new ContractionMeta
                        {
                            VariableName = context.Variable,
                            Text = context.Text,
                            Voice = context.Spoken
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
