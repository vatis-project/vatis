using System;
using System.Collections.Generic;
using System.Reactive;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class StaticDefinitionEditorDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> SaveButtonCommand { get; }

    private DialogResult _dialogResult;

    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    private string? _title = "Definition Editor";
    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string? DefinitionText
    {
        get => _textDocument?.Text;
        set => TextDocument = new TextDocument(value);
    }

    private TextDocument? _textDocument = new();
    public TextDocument? TextDocument
    {
        get => _textDocument;
        set => this.RaiseAndSetIfChanged(ref _textDocument, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    private string? _dataValidation;
    public string? DataValidation
    {
        get => _dataValidation;
        set => this.RaiseAndSetIfChanged(ref _dataValidation, value);
    }

    public StaticDefinitionEditorDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        SaveButtonCommand = ReactiveCommand.Create<ICloseable>(HandleSaveButton);
    }

    private void HandleSaveButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        SaveButtonCommand.Dispose();
    }
}
