using ReactiveUI;
using System;
using System.Reactive;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class UserInputDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    private string _title = "";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string _prompt = "";
    public string Prompt
    {
        get => _prompt;
        set => this.RaiseAndSetIfChanged(ref _prompt, value);
    }

    private string? _userValue;
    public string? UserValue
    {
        get => _userValue;
        set => this.RaiseAndSetIfChanged(ref _userValue, value);
    }

    private bool _forceUppercase;
    public bool ForceUppercase
    {
        get => _forceUppercase;
        set => this.RaiseAndSetIfChanged(ref _forceUppercase, value);
    }

    public UserInputDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCloseButton);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButton);
    }

    public void SetError(string error)
    {
        RaiseError(nameof(UserValue), error);
    }

    public void ClearError()
    {
        ClearErrors(nameof(UserValue));
    }

    private void HandleOkButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCloseButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
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
