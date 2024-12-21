using ReactiveUI;
using System;
using System.Reactive;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class UserInputDialogViewModel : ReactiveViewModelBase
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    private string mTitle = "";
    public string Title
    {
        get => mTitle;
        set => this.RaiseAndSetIfChanged(ref mTitle, value);
    }

    private string mPrompt = "";
    public string Prompt
    {
        get => mPrompt;
        set => this.RaiseAndSetIfChanged(ref mPrompt, value);
    }

    private string? mUserValue;
    public string? UserValue
    {
        get => mUserValue;
        set => this.RaiseAndSetIfChanged(ref mUserValue, value);
    }

    private bool mForceUppercase;
    public bool ForceUppercase
    {
        get => mForceUppercase;
        set => this.RaiseAndSetIfChanged(ref mForceUppercase, value);
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
}
