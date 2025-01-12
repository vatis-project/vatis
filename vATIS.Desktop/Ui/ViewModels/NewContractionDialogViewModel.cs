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
/// Represents the ViewModel for the New Contraction dialog.
/// </summary>
public class NewContractionDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private DialogResult _dialogResult;
    private string? _variable;
    private string? _text;
    private string? _spoken;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewContractionDialogViewModel"/> class.
    /// </summary>
    public NewContractionDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
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
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the variable associated with the contraction.
    /// </summary>
    public string? Variable
    {
        get => _variable;
        set => this.RaiseAndSetIfChanged(ref _variable, value);
    }

    /// <summary>
    /// Gets or sets the text associated with the contraction in the dialog.
    /// </summary>
    public string? Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    /// <summary>
    /// Gets or sets the spoken representation of the contraction used for voice synthesis.
    /// </summary>
    public string? Spoken
    {
        get => _spoken;
        set => this.RaiseAndSetIfChanged(ref _spoken, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();
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
}
