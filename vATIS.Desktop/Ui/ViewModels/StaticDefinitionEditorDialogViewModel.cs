// <copyright file="StaticDefinitionEditorDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using ReactiveUI;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Common;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the ViewModel for the Static Definition Editor Dialog.
/// Provides functionality for editing static definitions with data-binding support.
/// </summary>
public class StaticDefinitionEditorDialogViewModel : ReactiveViewModelBase, IDisposable, ISpokenTextSource
{
    private TextDocument? _textDocument = new();
    private List<ICompletionData> _contractionCompletionData = [];
    private DialogResult _dialogResult;
    private string? _title = "Definition Editor";
    private string? _dataValidation;
    private string? _separatorAfter;
    private StationCondition _stationCondition;
    private string? _stationCallsigns;
    private string? _weatherCondition;
    private string? _weatherConditionError;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinitionEditorDialogViewModel"/> class.
    /// </summary>
    public StaticDefinitionEditorDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        SaveButtonCommand = ReactiveCommand.Create<ICloseable>(HandleSaveButton);
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
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the title of the editor dialog.
    /// </summary>
    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the definition text.
    /// </summary>
    public string? DefinitionText
    {
        get => _textDocument?.Text;
        set => TextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the instance of the <see cref="AvaloniaEdit.Document.TextDocument"/> associated with this view model.
    /// This document represents the editable text content used in the dialog.
    /// </summary>
    public TextDocument? TextDocument
    {
        get => _textDocument;
        set => this.RaiseAndSetIfChanged(ref _textDocument, value);
    }

    /// <summary>
    /// Gets or sets the list of completion data used for contraction suggestions in the editor.
    /// This allows the editor to provide relevant completion suggestions when editing definitions.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the source that provides the built-in contractions and spoken text for the editor.
    /// </summary>
    public ISpokenTextSource? SpokenTextSource { get; set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> BuiltInContractions =>
        SpokenTextSource?.BuiltInContractions ?? new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets an optional separator placed after this definition. Blank uses the default separator.
    /// </summary>
    public string? SeparatorAfter
    {
        get => _separatorAfter;
        set => this.RaiseAndSetIfChanged(ref _separatorAfter, value);
    }

    /// <summary>
    /// Gets or sets when this definition is included, based on which controllers are online.
    /// </summary>
    public StationCondition StationCondition
    {
        get => _stationCondition;
        set
        {
            this.RaiseAndSetIfChanged(ref _stationCondition, value);
            this.RaisePropertyChanged(nameof(StationConditionIndex));
            this.RaisePropertyChanged(nameof(HasStationCondition));
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="StationCondition"/> as the index of the condition list.
    /// </summary>
    public int StationConditionIndex
    {
        get => (int)_stationCondition;
        set => StationCondition = (StationCondition)value;
    }

    /// <summary>
    /// Gets a value indicating whether the definition depends on controllers being online.
    /// </summary>
    public bool HasStationCondition => _stationCondition != StationCondition.Always;

    /// <summary>
    /// Gets or sets the callsigns the station condition applies to, separated by commas or spaces.
    /// </summary>
    public string? StationCallsigns
    {
        get => _stationCallsigns;
        set => this.RaiseAndSetIfChanged(ref _stationCallsigns, value);
    }

    /// <summary>
    /// Gets or sets the condition on the METAR, as text, e.g. <c>ceiling &lt; 200ft AND rvr &lt; 550m</c>.
    /// </summary>
    public string? WeatherCondition
    {
        get => _weatherCondition;
        set
        {
            this.RaiseAndSetIfChanged(ref _weatherCondition, value);
            WeatherConditionError = string.IsNullOrWhiteSpace(value) || WeatherExpression.TryParse(value, out _, out var error)
                ? null
                : error;
        }
    }

    /// <summary>
    /// Gets the problem with the weather condition as typed, or null if it is empty or valid.
    /// </summary>
    public string? WeatherConditionError
    {
        get => _weatherConditionError;
        private set
        {
            this.RaiseAndSetIfChanged(ref _weatherConditionError, value);
            this.RaisePropertyChanged(nameof(HasWeatherConditionError));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the weather condition has a problem.
    /// </summary>
    public bool HasWeatherConditionError => _weatherConditionError != null;

    /// <summary>
    /// Gets or sets the validation message associated with data input. This property provides feedback
    /// or error messages related to the data validation process.
    /// </summary>
    public string? DataValidation
    {
        get => _dataValidation;
        set => this.RaiseAndSetIfChanged(ref _dataValidation, value);
    }

    /// <summary>
    /// Loads the station and weather conditions of an existing definition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    public void LoadConditionsFrom(StaticDefinition definition)
    {
        StationCondition = definition.StationCondition;
        StationCallsigns = definition.StationCallsigns;
        WeatherCondition = definition.WeatherCondition;
    }

    /// <summary>
    /// Copies the station and weather conditions being edited onto a definition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The same definition.</returns>
    public StaticDefinition ApplyConditionsTo(StaticDefinition definition)
    {
        definition.StationCondition = StationCondition;
        definition.StationCallsigns = StationCallsigns?.Trim();
        definition.WeatherCondition = string.IsNullOrWhiteSpace(WeatherCondition) ? null : WeatherCondition.Trim();
        return definition;
    }

    /// <summary>
    /// Raises validation errors for incomplete station or weather conditions.
    /// </summary>
    public void ValidateConditions()
    {
        if (HasStationCondition && string.IsNullOrWhiteSpace(StationCallsigns))
        {
            RaiseError("DataValidation", "Enter at least one callsign.");
        }

        if (!string.IsNullOrWhiteSpace(WeatherCondition) && !WeatherExpression.TryParse(WeatherCondition, out _, out var error))
        {
            RaiseError("DataValidation", $"Weather condition: {error}");
        }
    }

    /// <inheritdoc/>
    public string? GetSpokenText(string token)
    {
        return SpokenTextSource?.GetSpokenText(token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        SaveButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleSaveButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }
}
