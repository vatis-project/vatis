// <copyright file="NewContractionDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the ViewModel for the New Contraction Dialog, encapsulating properties, commands,
/// and logic necessary to handle user interactions within the dialog.
/// </summary>
public class NewContractionDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private DialogResult dialogResult;
    private string? spoken;
    private string? text;
    private string? variable;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewContractionDialogViewModel"/> class.
    /// </summary>
    public NewContractionDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButtonCommand);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButtonCommand);
    }

    /// <summary>
    /// Occurs when the dialog result changes, indicating a new <see cref="DialogResult"/> value.
    /// </summary>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command executed when the Cancel button is clicked in the dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command executed when the OK button is clicked in the dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    /// <summary>
    /// Gets or sets the result of the dialog operation, indicating the outcome such as Ok or Cancel.
    /// </summary>
    public DialogResult DialogResult
    {
        get => this.dialogResult;
        set => this.RaiseAndSetIfChanged(ref this.dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the variable associated with the contraction.
    /// </summary>
    public string? Variable
    {
        get => this.variable;
        set => this.RaiseAndSetIfChanged(ref this.variable, value);
    }

    /// <summary>
    /// Gets or sets the text associated with the contraction in the dialog.
    /// </summary>
    public string? Text
    {
        get => this.text;
        set => this.RaiseAndSetIfChanged(ref this.text, value);
    }

    /// <summary>
    /// Gets or sets the spoken representation of the contraction used for voice synthesis.
    /// </summary>
    public string? Spoken
    {
        get => this.spoken;
        set => this.RaiseAndSetIfChanged(ref this.spoken, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

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
