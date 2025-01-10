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
    private readonly IAppConfig _appConfig;
    private readonly IWindowFactory _windowFactory;
    private IDialogOwner? _dialogOwner;

    public ContractionsViewModel(IWindowFactory windowFactory, IAppConfig appConfig)
    {
        this._windowFactory = windowFactory;
        this._appConfig = appConfig;

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleAtisStationChanged);
        this.CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(this.HandleCellEditEnding);
        this.NewContractionCommand = ReactiveCommand.CreateFromTask(this.HandleNewContraction);
        this.DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(this.HandleDeleteContraction);
    }

    public List<Tuple<int, ContractionMeta>> CurrentContractions { get; private set; } = [];

    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }

    public ReactiveCommand<Unit, Unit> NewContractionCommand { get; }

    public ReactiveCommand<ContractionMeta, Unit> DeleteContractionCommand { get; }

    public void SetDialogOwner(IDialogOwner? dialogOwner)
    {
        this._dialogOwner = dialogOwner;
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
        if (item == null || this.Contractions == null || this._dialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this._dialogOwner,
                "Are you sure you want to delete the selected contraction?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this.Contractions.Remove(item))
            {
                this.SelectedStation.Contractions.Remove(item);
                this._appConfig.SaveConfig();
                MessageBus.Current.SendMessage(new ContractionsUpdated(this.SelectedStation.Id));
            }
        }
    }

    private async Task HandleNewContraction()
    {
        if (this._dialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousVariableValue = "";
            var previousTextValue = "";
            var previousSpokenValue = "";

            var dialog = this._windowFactory.CreateNewContractionDialog();
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
                                Voice = context.Spoken
                            });
                        this._appConfig.SaveConfig();

                        this.Contractions = [];
                        foreach (var item in this.SelectedStation.Contractions)
                        {
                            this.Contractions.Add(item);
                        }

                        MessageBus.Current.SendMessage(new ContractionsUpdated(this.SelectedStation.Id));
                    }
                };
                await dialog.ShowDialog((Window)this._dialogOwner);
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

    #region Reactive Properties

    private AtisStation? _selectedStation;

    private AtisStation? SelectedStation
    {
        get => this._selectedStation;
        set => this.RaiseAndSetIfChanged(ref this._selectedStation, value);
    }

    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => this._hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref this._hasUnsavedChanges, value);
    }

    private ObservableCollection<ContractionMeta>? _contractions;

    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => this._contractions;
        set => this.RaiseAndSetIfChanged(ref this._contractions, value);
    }

    #endregion
}