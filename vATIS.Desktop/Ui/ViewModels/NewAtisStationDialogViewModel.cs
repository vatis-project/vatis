using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class NewAtisStationDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private string? _airportIdentifier;

    private AtisType _atisType = AtisType.Combined;

    private DialogResult _dialogResult;

    private string? _stationName;

    public NewAtisStationDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButtonCommand);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButtonCommand);
    }

    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    public DialogResult DialogResult
    {
        get => this._dialogResult;
        set => this.RaiseAndSetIfChanged(ref this._dialogResult, value);
    }

    public string? AirportIdentifier
    {
        get => this._airportIdentifier;
        set => this.RaiseAndSetIfChanged(ref this._airportIdentifier, value);
    }

    public string? StationName
    {
        get => this._stationName;
        set => this.RaiseAndSetIfChanged(ref this._stationName, value);
    }

    public AtisType AtisType
    {
        get => this._atisType;
        set => this.RaiseAndSetIfChanged(ref this._atisType, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    public event EventHandler<DialogResult>? DialogResultChanged;

    private void HandleOkButtonCommand(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        this.DialogResult = DialogResult.Ok;
        if (!this.HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        this.DialogResult = DialogResult.Cancel;
        window.Close();
    }
}