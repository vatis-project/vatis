// <copyright file="StaticDefinitionEditorDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the ViewModel for the Static Definition Editor Dialog. Provides functionality for editing static definitions with data-binding support.
/// </summary>
public class StaticDefinitionEditorDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private List<ICompletionData> contractionCompletionData = [];
    private string? dataValidation;
    private DialogResult dialogResult;
    private TextDocument? textDocument = new();
    private string? title = "Definition Editor";

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinitionEditorDialogViewModel"/> class.
    /// </summary>
    public StaticDefinitionEditorDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        this.SaveButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleSaveButton);
    }

    /// <summary>
    /// Occurs when the dialog result changes. This event is triggered to notify subscribers about updates
    /// to the <see cref="DialogResult"/> property.
    /// </summary>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command executed when the cancel button is clicked. This command triggers the close operation
    /// for an implementing <see cref="ICloseable"/> target.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command executed when the save button is clicked. This command initiates
    /// the saving operation and closes the associated <see cref="ICloseable"/> target.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveButtonCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating the result of a dialog operation.
    /// </summary>
    public DialogResult DialogResult
    {
        get => this.dialogResult;
        set => this.RaiseAndSetIfChanged(ref this.dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the title of the editor dialog.
    /// </summary>
    public string? Title
    {
        get => this.title;
        set => this.RaiseAndSetIfChanged(ref this.title, value);
    }

    /// <summary>
    /// Gets or sets the definition text.
    /// </summary>
    public string? DefinitionText
    {
        get => this.textDocument?.Text;
        set => this.TextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the instance of the <see cref="AvaloniaEdit.Document.TextDocument"/> associated with this view model.
    /// This document represents the editable text content used in the dialog.
    /// </summary>
    public TextDocument? TextDocument
    {
        get => this.textDocument;
        set => this.RaiseAndSetIfChanged(ref this.textDocument, value);
    }

    /// <summary>
    /// Gets or sets the list of completion data used for contraction suggestions in the editor.
    /// This allows the editor to provide relevant completion suggestions when editing definitions.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => this.contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this.contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the validation message associated with data input. This property provides feedback
    /// or error messages related to the data validation process.
    /// </summary>
    public string? DataValidation
    {
        get => this.dataValidation;
        set => this.RaiseAndSetIfChanged(ref this.dataValidation, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.SaveButtonCommand.Dispose();
    }

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
