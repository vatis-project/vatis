// <copyright file="TransitionLevelDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the Transition Level dialog.
/// </summary>
public class TransitionLevelDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private DialogResult dialogResult;
    private string? qnhHigh;
    private string? qnhLow;
    private string? transitionLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionLevelDialogViewModel"/> class.
    /// </summary>
    public TransitionLevelDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButton);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButton);
    }

    /// <summary>
    /// Occurs when the result of the dialog changes.
    /// </summary>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command that is executed when the cancel button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command that is executed when the OK button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    /// <summary>
    /// Gets or sets the lower QNH value in the transition level configuration.
    /// </summary>
    public string? QnhLow
    {
        get => this.qnhLow;
        set => this.RaiseAndSetIfChanged(ref this.qnhLow, value);
    }

    /// <summary>
    /// Gets or sets the high QNH value associated with the transition level dialog.
    /// </summary>
    public string? QnhHigh
    {
        get => this.qnhHigh;
        set => this.RaiseAndSetIfChanged(ref this.qnhHigh, value);
    }

    /// <summary>
    /// Gets or sets the transition level entered or displayed in the dialog.
    /// </summary>
    public string? TransitionLevel
    {
        get => this.transitionLevel;
        set => this.RaiseAndSetIfChanged(ref this.transitionLevel, value);
    }

    /// <summary>
    /// Gets or sets the result of the dialog.
    /// </summary>
    public DialogResult DialogResult
    {
        get => this.dialogResult;
        set => this.RaiseAndSetIfChanged(ref this.dialogResult, value);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="TransitionLevelDialogViewModel"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if an operation is performed on a disposed instance.
    /// </exception>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DialogResultChanged = null;
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    private void HandleOkButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        this.DialogResult = DialogResult.Ok;
        if (!this.HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButton(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        this.DialogResult = DialogResult.Cancel;
        window.Close();
    }
}
