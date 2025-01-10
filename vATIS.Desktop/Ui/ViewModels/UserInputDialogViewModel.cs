// <copyright file="UserInputDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a view model for a user input dialog, providing functionalities for user input handling and dialog control.
/// </summary>
public class UserInputDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private bool forceUppercase;
    private string prompt = string.Empty;
    private string title = string.Empty;
    private string? userValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputDialogViewModel"/> class.
    /// </summary>
    public UserInputDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCloseButton);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButton);
    }

    /// <summary>
    /// Occurs when the dialog result changes, indicating a user action such as confirming or canceling the dialog.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Raised when the event is invoked with a null sender or dialog result.
    /// </exception>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command that is executed when the Cancel button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command that is executed when the OK button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    /// <summary>
    /// Gets or sets the title of the dialog.
    /// </summary>
    public string Title
    {
        get => this.title;
        set => this.RaiseAndSetIfChanged(ref this.title, value);
    }

    /// <summary>
    /// Gets or sets the text displayed as a prompt in the input dialog.
    /// </summary>
    public string Prompt
    {
        get => this.prompt;
        set => this.RaiseAndSetIfChanged(ref this.prompt, value);
    }

    /// <summary>
    /// Gets or sets the value entered by the user in the input dialog.
    /// </summary>
    public string? UserValue
    {
        get => this.userValue;
        set => this.RaiseAndSetIfChanged(ref this.userValue, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether user input should be automatically converted to uppercase.
    /// </summary>
    public bool ForceUppercase
    {
        get => this.forceUppercase;
        set => this.RaiseAndSetIfChanged(ref this.forceUppercase, value);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="UserInputDialogViewModel"/> class.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when an operation is performed on a disposed instance.
    /// </exception>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    /// <summary>
    /// Sets an error message for the specified property.
    /// </summary>
    /// <param name="error">The error message to be displayed for the property.</param>
    public void SetError(string error)
    {
        this.RaiseError(nameof(this.UserValue), error);
    }

    /// <summary>
    /// Clears the current error associated with the property "UserValue".
    /// </summary>
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
