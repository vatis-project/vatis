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
    private readonly IDisposable _sampleGeneration;
    private DialogResult _dialogResult;
    private string? _text;
    private string? _spoken;
    private bool _addToAllStations;
    private string _title = "New Pattern";
    private bool _showAddToAllStations = true;
    private string? _pattern;
    private string? _sampleText;
    private bool _sampleEditedByUser;
    private bool _settingSampleFromPattern;
    private bool _hasPreview;
    private string _previewStatus = string.Empty;
    private string _previewText = string.Empty;
    private string _previewVoice = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewPatternDialogViewModel"/> class.
    /// </summary>
    public NewPatternDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
        NewSampleCommand = ReactiveCommand.Create(() => GenerateSample(Random.Shared, userRequested: true));

        // Fill the preview with a sample taken from the pattern until the user types their own.
        _sampleGeneration = this.WhenAnyValue(x => x.Pattern)
            .Subscribe(_ =>
            {
                if (!_sampleEditedByUser)
                {
                    GenerateSample(string.IsNullOrEmpty(Pattern) ? Random.Shared : PatternSampleGenerator.SeededFor(Pattern), userRequested: false);
                }
            });

        // Explain regex mistakes as the user types.
        _validation = this.WhenAnyValue(x => x.Pattern, x => x.Text, x => x.Spoken, x => x.SampleText)
            .Subscribe(_ =>
            {
                ValidateRegex();
                UpdatePreview();
            });
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
    /// Gets or sets the sample text the pattern is previewed against.
    /// </summary>
    public string? SampleText
    {
        get => _sampleText;
        set
        {
            // Typing a sample stops it being replaced from the pattern; clearing the box resumes that.
            if (!_settingSampleFromPattern)
            {
                _sampleEditedByUser = !string.IsNullOrEmpty(value);
            }

            this.RaiseAndSetIfChanged(ref _sampleText, value);
        }
    }

    /// <summary>
    /// Gets the command that replaces the sample text with another one generated from the pattern.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewSampleCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern matches the sample text, so a result can be shown.
    /// </summary>
    public bool HasPreview
    {
        get => _hasPreview;
        private set => this.RaiseAndSetIfChanged(ref _hasPreview, value);
    }

    /// <summary>
    /// Gets a message explaining why there is no preview.
    /// </summary>
    public string PreviewStatus
    {
        get => _previewStatus;
        private set => this.RaiseAndSetIfChanged(ref _previewStatus, value);
    }

    /// <summary>
    /// Gets the sample text after the Text replacement.
    /// </summary>
    public string PreviewText
    {
        get => _previewText;
        private set => this.RaiseAndSetIfChanged(ref _previewText, value);
    }

    /// <summary>
    /// Gets the sample text after the Voice replacement.
    /// </summary>
    public string PreviewVoice
    {
        get => _previewVoice;
        private set => this.RaiseAndSetIfChanged(ref _previewVoice, value);
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
        _sampleGeneration.Dispose();
        NewSampleCommand.Dispose();
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void GenerateSample(Random random, bool userRequested)
    {
        _settingSampleFromPattern = true;
        try
        {
            SampleText = PatternSampleGenerator.TryGenerate(Pattern, random, out var sample) ? sample : string.Empty;
            if (userRequested)
            {
                _sampleEditedByUser = false;
            }
        }
        finally
        {
            _settingSampleFromPattern = false;
        }
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(Pattern))
        {
            HasPreview = false;
            PreviewStatus = "Enter a pattern to see a preview.";
            return;
        }

        if (PatternMatcher.Validate(Pattern, null, null).Count > 0)
        {
            HasPreview = false;
            PreviewStatus = "Fix the pattern to see a preview.";
            return;
        }

        if (string.IsNullOrEmpty(SampleText))
        {
            HasPreview = false;
            PreviewStatus = "Type some sample text to preview the result.";
            return;
        }

        var matched = PatternMatcher.TryPreview(Pattern, Text, Spoken, SampleText, out var text, out var voice);
        HasPreview = matched;
        PreviewText = text;
        PreviewVoice = voice;
        PreviewStatus = matched ? string.Empty : "The pattern does not match this sample text.";
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
