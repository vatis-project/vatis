using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class TransitionLevelDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private DialogResult _dialogResult;

    private string? _qnhHigh;

    private string? _qnhLow;

    private string? _transitionLevel;

    public TransitionLevelDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButton);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButton);
    }

    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    public string? QnhLow
    {
        get => this._qnhLow;
        set => this.RaiseAndSetIfChanged(ref this._qnhLow, value);
    }

    public string? QnhHigh
    {
        get => this._qnhHigh;
        set => this.RaiseAndSetIfChanged(ref this._qnhHigh, value);
    }

    public string? TransitionLevel
    {
        get => this._transitionLevel;
        set => this.RaiseAndSetIfChanged(ref this._transitionLevel, value);
    }

    public DialogResult DialogResult
    {
        get => this._dialogResult;
        set => this.RaiseAndSetIfChanged(ref this._dialogResult, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    public event EventHandler<DialogResult>? DialogResultChanged;

    private void HandleOkButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        this.DialogResult = DialogResult.Ok;
        if (!this.HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        this.DialogResult = DialogResult.Cancel;
        window.Close();
    }
}