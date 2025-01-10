using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class NewContractionDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private DialogResult _dialogResult;

    private string? _spoken;

    private string? _text;

    private string? _variable;

    public NewContractionDialogViewModel()
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

    public string? Variable
    {
        get => this._variable;
        set => this.RaiseAndSetIfChanged(ref this._variable, value);
    }

    public string? Text
    {
        get => this._text;
        set => this.RaiseAndSetIfChanged(ref this._text, value);
    }

    public string? Spoken
    {
        get => this._spoken;
        set => this.RaiseAndSetIfChanged(ref this._spoken, value);
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