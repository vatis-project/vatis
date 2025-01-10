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
    private List<ICompletionData> _contractionCompletionData = [];

    private string? _dataValidation;

    private DialogResult _dialogResult;

    private TextDocument? _textDocument = new();

    private string? _title = "Definition Editor";

    public StaticDefinitionEditorDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        this.SaveButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleSaveButton);
    }

    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> SaveButtonCommand { get; }

    public DialogResult DialogResult
    {
        get => this._dialogResult;
        set => this.RaiseAndSetIfChanged(ref this._dialogResult, value);
    }

    public string? Title
    {
        get => this._title;
        set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    public string? DefinitionText
    {
        get => this._textDocument?.Text;
        set => this.TextDocument = new TextDocument(value);
    }

    public TextDocument? TextDocument
    {
        get => this._textDocument;
        set => this.RaiseAndSetIfChanged(ref this._textDocument, value);
    }

    public List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
    }

    public string? DataValidation
    {
        get => this._dataValidation;
        set => this.RaiseAndSetIfChanged(ref this._dataValidation, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.SaveButtonCommand.Dispose();
    }

    public event EventHandler<DialogResult>? DialogResultChanged;

    private void HandleSaveButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        this.DialogResult = DialogResult.Ok;
        if (!this.HasErrors)
        {
            window.Close();
        }
    }
}