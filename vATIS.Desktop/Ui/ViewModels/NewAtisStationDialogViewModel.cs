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

    private DialogResult mDialogResult;
    public DialogResult DialogResult
    {
        get => mDialogResult;
        set => this.RaiseAndSetIfChanged(ref mDialogResult, value);
    }

    private string? mAirportIdentifier;
    public string? AirportIdentifier
    {
        get => mAirportIdentifier;
        set => this.RaiseAndSetIfChanged(ref mAirportIdentifier, value);
    }

    private string? mStationName;
    public string? StationName
    {
        get => mStationName;
        set => this.RaiseAndSetIfChanged(ref mStationName, value);
    }

    private AtisType mAtisType = AtisType.Combined;
    public AtisType AtisType
    {
        get => mAtisType;
        set => this.RaiseAndSetIfChanged(ref mAtisType, value);
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
