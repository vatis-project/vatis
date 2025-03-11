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
    private string _title = "";
    private string _prompt = "";
    private string? _userValue;
    private bool _forceUppercase;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputDialogViewModel"/> class.
    /// </summary>
    public UserInputDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCloseButton);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButton);
    }

    /// <summary>
    /// Occurs when the dialog result changes, indicating a user action such as confirming or canceling the dialog.
    /// </summary>
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
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the text displayed as a prompt in the input dialog.
    /// </summary>
    public string Prompt
    {
        get => _prompt;
        set => this.RaiseAndSetIfChanged(ref _prompt, value);
    }

    /// <summary>
    /// Gets or sets the value entered by the user in the input dialog.
    /// </summary>
    public string? UserValue
    {
        get => _userValue;
        set => this.RaiseAndSetIfChanged(ref _userValue, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether user input should be automatically converted to uppercase.
    /// </summary>
    public bool ForceUppercase
    {
        get => _forceUppercase;
        set => this.RaiseAndSetIfChanged(ref _forceUppercase, value);
    }

    /// <summary>
    /// Sets an error message for the specified property.
    /// </summary>
    /// <param name="error">The error message to be displayed for the property.</param>
    public void SetError(string error)
    {
        RaiseError(nameof(UserValue), error);
    }

    /// <summary>
    /// Clears the current error associated with the property "UserValue".
    /// </summary>
    public void ClearError()
    {
        ClearErrors(nameof(UserValue));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleOkButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCloseButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        window.Close();
    }
}
