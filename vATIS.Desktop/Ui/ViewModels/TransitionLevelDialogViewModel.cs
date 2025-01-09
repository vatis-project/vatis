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

    private string? _qnhLow;
    public string? QnhLow
    {
        get => _qnhLow;
        set => this.RaiseAndSetIfChanged(ref _qnhLow, value);
    }

    private string? _qnhHigh;
    public string? QnhHigh
    {
        get => _qnhHigh;
        set => this.RaiseAndSetIfChanged(ref _qnhHigh, value);
    }

    private string? _transitionLevel;
    public string? TransitionLevel
    {
        get => _transitionLevel;
        set => this.RaiseAndSetIfChanged(ref _transitionLevel, value);
    }

    private DialogResult _dialogResult;
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
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
