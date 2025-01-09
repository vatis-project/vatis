using ReactiveUI;
using System;
using System.Reactive;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class NewAtisStationDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    private DialogResult _dialogResult;
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    private string? _airportIdentifier;
    public string? AirportIdentifier
    {
        get => _airportIdentifier;
        set => this.RaiseAndSetIfChanged(ref _airportIdentifier, value);
    }

    private string? _stationName;
    public string? StationName
    {
        get => _stationName;
        set => this.RaiseAndSetIfChanged(ref _stationName, value);
    }

    private AtisType _atisType = AtisType.Combined;
    public AtisType AtisType
    {
        get => _atisType;
        set => this.RaiseAndSetIfChanged(ref _atisType, value);
    }

    public NewAtisStationDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
    }

    private void HandleOkButtonCommand(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        DialogResult = DialogResult.Cancel;
        window.Close();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();
    }
}
