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

    private DialogResult mDialogResult;

    public DialogResult DialogResult
    {
        get => mDialogResult;
        set => this.RaiseAndSetIfChanged(ref mDialogResult, value);
    }

    private string? mTitle = "Definition Editor";

    public string? Title
    {
        get => mTitle;
        set => this.RaiseAndSetIfChanged(ref mTitle, value);
    }
    
    public string? DefinitionText
    {
        get => mTextDocument?.Text;
        set => TextDocument = new TextDocument(value);
    }
    
    private TextDocument? mTextDocument = new();
    public TextDocument? TextDocument
    {
        get => mTextDocument;
        set => this.RaiseAndSetIfChanged(ref mTextDocument, value);
    }
    
    private List<ICompletionData> mContractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => mContractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref mContractionCompletionData, value);
    }
    
    private string? mDataValidation;
    public string? DataValidation
    {
        get => mDataValidation;
        set => this.RaiseAndSetIfChanged(ref mDataValidation, value);
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