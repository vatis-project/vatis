// <copyright file="NewPatternDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the ViewModel for the New Pattern dialog.
/// </summary>
public class NewPatternDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IDisposable _validation;
    private DialogResult _dialogResult;
    private string? _text;
    private string? _spoken;
    private bool _addToAllStations;
    private string _title = "New Pattern";
    private bool _showAddToAllStations = true;
    private string? _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewPatternDialogViewModel"/> class.
    /// </summary>
    public NewPatternDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);

        // Explain regex mistakes as the user types.
        _validation = this.WhenAnyValue(x => x.Pattern, x => x.Text, x => x.Spoken)
            .Subscribe(_ => ValidateRegex());
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
    /// Gets or sets the title shown at the top of the dialog.
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "add to all stations" option is shown.
    /// </summary>
    public bool ShowAddToAllStations
    {
        get => _showAddToAllStations;
        set => this.RaiseAndSetIfChanged(ref _showAddToAllStations, value);
    }

    /// <summary>
    /// Gets or sets the text associated in the dialog.
    /// </summary>
    public string? Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    /// <summary>
    /// Gets or sets the spoken replacement of the pattern, used for voice synthesis.
    /// </summary>
    public string? Spoken
    {
        get => _spoken;
        set => this.RaiseAndSetIfChanged(ref _spoken, value);
    }

    /// <summary>
    /// Gets or sets the regular expression matched against the ATIS text.
    /// </summary>
    public string? Pattern
    {
        get => _pattern;
        set => this.RaiseAndSetIfChanged(ref _pattern, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pattern should be added to all ATIS stations in the profile.
    /// </summary>
    public bool AddToAllStations
    {
        get => _addToAllStations;
        set => this.RaiseAndSetIfChanged(ref _addToAllStations, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _validation.Dispose();
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void ValidateRegex()
    {
        ClearErrors("Pattern");
        ClearErrors("Text");
        ClearErrors("Spoken");

        foreach (var (field, message) in PatternMatcher.Validate(Pattern, Text, Spoken))
        {
            RaiseError(field == "Voice" ? "Spoken" : field, message);
        }
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
