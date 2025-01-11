// <copyright file="NewContractionDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

public class NewContractionDialogViewModel : ReactiveViewModelBase, IDisposable
{
    public event EventHandler<DialogResult>? DialogResultChanged;
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    private DialogResult _dialogResult;
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    private string? _variable;
    public string? Variable
    {
        get => _variable;
        set => this.RaiseAndSetIfChanged(ref _variable, value);
    }

    private string? _text;
    public string? Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private string? _spoken;
    public string? Spoken
    {
        get => _spoken;
        set => this.RaiseAndSetIfChanged(ref _spoken, value);
    }

    public NewContractionDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();
    }
}
