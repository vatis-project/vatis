using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class TransitionLevelDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    private string? mQnhLow;
    public string? QnhLow
    {
        get => mQnhLow;
        set => this.RaiseAndSetIfChanged(ref mQnhLow, value);
    }

    private string? mQnhHigh;
    public string? QnhHigh
    {
        get => mQnhHigh;
        set => this.RaiseAndSetIfChanged(ref mQnhHigh, value);
    }

    private string? mTransitionLevel;
    public string? TransitionLevel
    {
        get => mTransitionLevel;
        set => this.RaiseAndSetIfChanged(ref mTransitionLevel, value);
    }

    private DialogResult mDialogResult;
    public DialogResult DialogResult
    {
        get => mDialogResult;
        set => this.RaiseAndSetIfChanged(ref mDialogResult, value);
    }

    public TransitionLevelDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButton);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButton);
    }

    private void HandleOkButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        DialogResult = DialogResult.Cancel;
        window.Close();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();
    }
}