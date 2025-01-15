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
    private string? _qnhLow;
    private string? _qnhHigh;
    private string? _transitionLevel;
    private DialogResult _dialogResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionLevelDialogViewModel"/> class.
    /// </summary>
    public TransitionLevelDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButton);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButton);
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
        get => _qnhLow;
        set => this.RaiseAndSetIfChanged(ref _qnhLow, value);
    }

    /// <summary>
    /// Gets or sets the high QNH value associated with the transition level dialog.
    /// </summary>
    public string? QnhHigh
    {
        get => _qnhHigh;
        set => this.RaiseAndSetIfChanged(ref _qnhHigh, value);
    }

    /// <summary>
    /// Gets or sets the transition level entered or displayed in the dialog.
    /// </summary>
    public string? TransitionLevel
    {
        get => _transitionLevel;
        set => this.RaiseAndSetIfChanged(ref _transitionLevel, value);
    }

    /// <summary>
    /// Gets or sets the result of the dialog.
    /// </summary>
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();
    }

    private void HandleOkButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        DialogResult = DialogResult.Cancel;
        window.Close();
    }
}
