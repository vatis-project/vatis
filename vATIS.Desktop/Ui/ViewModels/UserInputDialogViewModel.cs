using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class UserInputDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private bool _forceUppercase;

    private string _prompt = "";

    private string _title = "";

    private string? _userValue;

    public UserInputDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCloseButton);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButton);
    }

    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    public string Title
    {
        get => this._title;
        set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    public string Prompt
    {
        get => this._prompt;
        set => this.RaiseAndSetIfChanged(ref this._prompt, value);
    }

    public string? UserValue
    {
        get => this._userValue;
        set => this.RaiseAndSetIfChanged(ref this._userValue, value);
    }

    public bool ForceUppercase
    {
        get => this._forceUppercase;
        set => this.RaiseAndSetIfChanged(ref this._forceUppercase, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    public event EventHandler<DialogResult>? DialogResultChanged;

    public void SetError(string error)
    {
        this.RaiseError(nameof(this.UserValue), error);
    }

    public void ClearError()
    {
        this.ClearErrors(nameof(this.UserValue));
    }

    private void HandleOkButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        if (!this.HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCloseButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        window.Close();
    }
}