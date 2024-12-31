using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class NewContractionDialogViewModel : ReactiveViewModelBase, IDisposable
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

    private string? mVariable;
    public string? Variable
    {
        get => mVariable;
        set => this.RaiseAndSetIfChanged(ref mVariable, value);
    }
    
    private string? mText;
    public string? Text
    {
        get => mText;
        set => this.RaiseAndSetIfChanged(ref mText, value);
    }

    private string? mSpoken;
    public string? Spoken
    {
        get => mSpoken;
        set => this.RaiseAndSetIfChanged(ref mSpoken, value);
    }

    public NewContractionDialogViewModel()
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