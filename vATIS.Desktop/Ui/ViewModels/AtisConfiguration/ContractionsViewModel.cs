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
    private readonly IWindowFactory mWindowFactory;
    private readonly IAppConfig mAppConfig;
    private IDialogOwner? mDialogOwner;
    
    public List<Tuple<int, ContractionMeta>> CurrentContractions { get; set; } = [];
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }
    public ReactiveCommand<Unit, Unit> NewContractionCommand { get; }
    public ReactiveCommand<ContractionMeta, Unit> DeleteContractionCommand { get; }

    #region Reactive Properties
    private AtisStation? mSelectedStation;
    public AtisStation? SelectedStation
    {
        get => mSelectedStation;
        set => this.RaiseAndSetIfChanged(ref mSelectedStation, value);
    }
    
    private bool mHasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => mHasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref mHasUnsavedChanges, value);
    }
    
    private ObservableCollection<ContractionMeta>? mContractions;
    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => mContractions;
        set => this.RaiseAndSetIfChanged(ref mContractions, value);
    }
    #endregion

    public ContractionsViewModel(IWindowFactory windowFactory, IAppConfig appConfig)
    {
        mWindowFactory = windowFactory;
        mAppConfig = appConfig;
        
        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(HandleCellEditEnding);
        NewContractionCommand = ReactiveCommand.CreateFromTask(HandleNewContraction);
        DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(HandleDeleteContraction);
    }

    public void SetDialogOwner(IDialogOwner? dialogOwner)
    {
        mDialogOwner = dialogOwner;
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
        if (item == null || Contractions == null || mDialogOwner == null || SelectedStation == null)
            return;

        if (await MessageBox.ShowDialog((Window)mDialogOwner,
                "Are you sure you want to delete the selected contraction?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (Contractions.Remove(item))
            {
                SelectedStation.Contractions.Remove(item);
                mAppConfig.SaveConfig();
                MessageBus.Current.SendMessage(new ContractionsUpdated(SelectedStation.Id));
            }
        }
    }

    private async Task HandleNewContraction()
    {
        if (mDialogOwner == null || SelectedStation == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousVariableValue = "";
            var previousTextValue = "";
            var previousSpokenValue = "";

            var dialog = mWindowFactory.CreateNewContractionDialog();
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
                        mAppConfig.SaveConfig();

                        Contractions = [];
                        foreach (var item in SelectedStation.Contractions)
                        {
                            Contractions.Add(item);
                        }

                        MessageBus.Current.SendMessage(new ContractionsUpdated(SelectedStation.Id));
                    }
                };
                await dialog.ShowDialog((Window)mDialogOwner);
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