// <copyright file="GeneralConfigViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Utils;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Represents the general configuration view model for ATIS configurations.
/// Inherits from <see cref="ReactiveViewModelBase"/>.
/// </summary>
public class GeneralConfigViewModel : ReactiveViewModelBase, IDisposable
{
    private static readonly int[] s_allowedSpeechRates = [120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240];
    private readonly CompositeDisposable _disposables = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly Dictionary<string, (object? OriginalValue, object? CurrentValue)> _fieldHistory;
    private bool _hasUnsavedChanges;
    private int _selectedTabIndex;
    private AtisStation? _selectedStation;
    private string _profileSerialNumber = string.Empty;
    private string? _frequency;
    private AtisType _atisType = AtisType.Combined;
    private char _codeRangeLow = 'A';
    private char _codeRangeHigh = 'Z';
    private bool _useTextToSpeech;
    private string? _textToSpeechVoice;
    private bool _useDecimalTerminology;
    private string? _idsEndpoint;
    private ObservableCollection<VoiceMetaData>? _availableVoices;
    private bool _showDuplicateAtisTypeError;
    private int _selectedSpeechRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralConfigViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager responsible for managing current session data.</param>
    /// <param name="profileRepository">The profile repository for accessing and managing profile data.</param>
    public GeneralConfigViewModel(ISessionManager sessionManager, IProfileRepository profileRepository)
    {
        _sessionManager = sessionManager;
        _profileRepository = profileRepository;
        _fieldHistory = [];

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);

        _disposables.Add(AtisStationChanged);

        ProfileSerialNumber = _sessionManager.CurrentProfile?.UpdateSerial != null
            ? $"Profile Serial: {_sessionManager.CurrentProfile.UpdateSerial}"
            : string.Empty;
    }

    /// <summary>
    /// Gets the command that is triggered when the ATIS station changes.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

     /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets the selected tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        private set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the profile serial number.
    /// </summary>
    public string ProfileSerialNumber
    {
        get => _profileSerialNumber;
        set => this.RaiseAndSetIfChanged(ref _profileSerialNumber, value);
    }

    /// <summary>
    /// Gets or sets the frequency.
    /// </summary>
    public string? Frequency
    {
        get => _frequency;
        set
        {
            this.RaiseAndSetIfChanged(ref _frequency, value);
            TrackChanges(nameof(Frequency), value);
        }
    }

    /// <summary>
    /// Gets or sets the ATIS type.
    /// </summary>
    public AtisType AtisType
    {
        get => _atisType;
        set
        {
            this.RaiseAndSetIfChanged(ref _atisType, value);
            TrackChanges(nameof(AtisType), value);
        }
    }

    /// <summary>
    /// Gets or sets the low range of the code.
    /// </summary>
    public char CodeRangeLow
    {
        get => _codeRangeLow;
        set
        {
            this.RaiseAndSetIfChanged(ref _codeRangeLow, value);
            TrackChanges(nameof(CodeRangeLow), value);
        }
    }

    /// <summary>
    /// Gets or sets the high range of the code.
    /// </summary>
    public char CodeRangeHigh
    {
        get => _codeRangeHigh;
        set
        {
            this.RaiseAndSetIfChanged(ref _codeRangeHigh, value);
            TrackChanges(nameof(CodeRangeHigh), value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use text-to-speech.
    /// </summary>
    public bool UseTextToSpeech
    {
        get => _useTextToSpeech;
        set
        {
            this.RaiseAndSetIfChanged(ref _useTextToSpeech, value);
            TrackChanges(nameof(UseTextToSpeech), value);
        }
    }

    /// <summary>
    /// Gets or sets the text-to-speech voice.
    /// </summary>
    public string? TextToSpeechVoice
    {
        get => _textToSpeechVoice;
        set
        {
            if (_textToSpeechVoice == value || string.IsNullOrEmpty(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _textToSpeechVoice, value);
            TrackChanges(nameof(TextToSpeechVoice), value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use decimal terminology.
    /// </summary>
    public bool UseDecimalTerminology
    {
        get => _useDecimalTerminology;
        set
        {
            this.RaiseAndSetIfChanged(ref _useDecimalTerminology, value);
            TrackChanges(nameof(UseDecimalTerminology), value);
        }
    }

    /// <summary>
    /// Gets or sets the IDS endpoint.
    /// </summary>
    public string? IdsEndpoint
    {
        get => _idsEndpoint;
        set
        {
            this.RaiseAndSetIfChanged(ref _idsEndpoint, value);
            TrackChanges(nameof(IdsEndpoint), value);
        }
    }

    /// <summary>
    /// Gets or sets the available voices.
    /// </summary>
    public ObservableCollection<VoiceMetaData>? AvailableVoices
    {
        get => _availableVoices;
        set => this.RaiseAndSetIfChanged(ref _availableVoices, value);
    }

    /// <summary>
    /// Gets the available speech rate multipliers.
    /// </summary>
    public ObservableCollection<int> SpeechRates { get; } = new(s_allowedSpeechRates);

    /// <summary>
    /// Gets or sets the selected speech rate multiplier.
    /// </summary>
    public int SelectedSpeechRate
    {
        get => _selectedSpeechRate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSpeechRate, value);
            TrackChanges(nameof(SelectedSpeechRate), value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show duplicate ATIS type error.
    /// </summary>
    public bool ShowDuplicateAtisTypeError
    {
        get => _showDuplicateAtisTypeError;
        set => this.RaiseAndSetIfChanged(ref _showDuplicateAtisTypeError, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Resets the state of the <see cref="GeneralConfigViewModel"/> instance to its default configuration.
    /// </summary>
    public void Reset()
    {
        Frequency = null;
        AtisType = AtisType.Combined;
        CodeRangeLow = '\0';
        CodeRangeHigh = '\0';
        UseDecimalTerminology = false;
        UseTextToSpeech = true;
        IdsEndpoint = null;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Applies the general configuration settings for the current session and validates the input data.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the configuration was successfully applied.
    /// </returns>
    public bool ApplyConfig()
    {
        if (SelectedStation == null)
        {
            return false;
        }

        ClearAllErrors();
        ShowDuplicateAtisTypeError = false;

        if (string.IsNullOrEmpty(Frequency))
        {
            SelectedTabIndex = 0;
            RaiseError(nameof(Frequency), "Frequency is required.");
        }
        else
        {
            if (!FrequencyValidator.TryParseMHz(Frequency, out var parsedFrequency, out var error))
            {
                if (error != null)
                {
                    SelectedTabIndex = 0;
                    RaiseError(nameof(Frequency), error);
                }
            }
            else
            {
                if (parsedFrequency != SelectedStation.Frequency)
                {
                    SelectedStation.Frequency = parsedFrequency;
                }
            }
        }

        if (SelectedStation.AtisType != AtisType)
        {
            if (_sessionManager.CurrentProfile?.Stations != null &&
                _sessionManager.CurrentProfile.Stations.Any(
                    x =>
                        x != SelectedStation && x.Identifier == SelectedStation.Identifier &&
                        x.AtisType == AtisType))
            {
                ShowDuplicateAtisTypeError = true;
            }
            else
            {
                SelectedStation.AtisType = AtisType;
            }
        }

        if (CodeRangeLow == '\0' || CodeRangeHigh == '\0')
        {
            SelectedTabIndex = 0;
            RaiseError(nameof(CodeRangeHigh), "ATIS code range is required.");
        }
        else if (CodeRangeHigh < CodeRangeLow)
        {
            SelectedTabIndex = 0;
            RaiseError(nameof(CodeRangeHigh), "ATIS code range must be in sequential alphabetical order.");
        }

        var codeRange = new CodeRangeMeta(CodeRangeLow, CodeRangeHigh);

        if (SelectedStation.CodeRange != codeRange)
        {
            SelectedStation.CodeRange = codeRange;
        }

        if (SelectedStation.UseDecimalTerminology != UseDecimalTerminology)
        {
            SelectedStation.UseDecimalTerminology = UseDecimalTerminology;
        }

        if (SelectedStation.IdsEndpoint != IdsEndpoint)
        {
            SelectedStation.IdsEndpoint = IdsEndpoint ?? string.Empty;
        }

        if (SelectedStation.AtisVoice.UseTextToSpeech != UseTextToSpeech)
        {
            SelectedStation.AtisVoice.UseTextToSpeech = UseTextToSpeech;
            EventBus.Instance.Publish(new AtisVoiceTypeChanged(SelectedStation.Id, UseTextToSpeech));
        }

        if (SelectedStation.AtisVoice.Voice != TextToSpeechVoice)
        {
            SelectedStation.AtisVoice.Voice = TextToSpeechVoice;
        }

        if (SelectedStation.AtisVoice.SpeechRate != SelectedSpeechRate)
        {
            SelectedStation.AtisVoice.SpeechRate =
                s_allowedSpeechRates.Contains(SelectedSpeechRate) ? SelectedSpeechRate : 180;
        }

        if (HasErrors || ShowDuplicateAtisTypeError)
        {
            return false;
        }

        if (_sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        // Check if there are unsaved changes before saving
        var anyChanges = _fieldHistory.Any(entry => !AreValuesEqual(entry.Value.OriginalValue, entry.Value.CurrentValue));

        // If there are unsaved changes, mark as applied and reset HasUnsavedChanges
        if (anyChanges)
        {
            // Apply changes and reset HasUnsavedChanges
            foreach (var key in _fieldHistory.Keys.ToList())
            {
                var (_, currentValue) = _fieldHistory[key];

                // After applying changes, set the original value to the current value (the saved value)
                _fieldHistory[key] = (currentValue, currentValue);
            }

            HasUnsavedChanges = false;
            return true;
        }

        return false;
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        ClearAllErrors();
        ShowDuplicateAtisTypeError = false;

        _fieldHistory.Clear();

        SelectedTabIndex = -1;
        SelectedStation = station;
        Frequency = station.Frequency > 0
            ? (station.Frequency / 1000000.0).ToString("000.000", CultureInfo.GetCultureInfo("en-US"))
            : string.Empty;
        AtisType = station.AtisType;
        CodeRangeLow = station.CodeRange.Low;
        CodeRangeHigh = station.CodeRange.High;
        UseDecimalTerminology = station.UseDecimalTerminology;
        IdsEndpoint = station.IdsEndpoint;
        UseTextToSpeech = station.AtisVoice.UseTextToSpeech;
        TextToSpeechVoice = station.AtisVoice.Voice;

        // Ensure speech rate is a valid value.
        SelectedSpeechRate = s_allowedSpeechRates.Contains(station.AtisVoice.SpeechRate)
            ? station.AtisVoice.SpeechRate
            : 180; // Fallback to default speech rate value.

        HasUnsavedChanges = false;
    }

    private void TrackChanges(string propertyName, object? currentValue)
    {
        if (_fieldHistory.TryGetValue(propertyName, out var value))
        {
            var (originalValue, _) = value;

            // If the property has been set before, update the current value
            _fieldHistory[propertyName] = (originalValue, currentValue);

            // Check for unsaved changes after the update
            CheckForUnsavedChanges();
        }
        else
        {
            // If this is the first time setting the value, initialize the original value
            _fieldHistory[propertyName] = (currentValue, currentValue);

            // Check for unsaved changes after the update
            CheckForUnsavedChanges();
        }
    }

    private bool AreValuesEqual(object? originalValue, object? currentValue)
    {
        // If both are null, consider them equal
        if (originalValue == null && currentValue == null)
            return true;

        // If one is null and the other is not, they are not equal
        if (originalValue == null || currentValue == null)
            return false;

        // Handle comparison for nullable types like int?, string?, bool? etc.
        if (originalValue is string originalStr && currentValue is string currentStr)
        {
            return originalStr == currentStr;
        }

        // Compare nullable int (int?)
        if (originalValue is int originalInt && currentValue is int currentInt)
        {
            return originalInt == currentInt;
        }

        // Compare nullable bool (bool?)
        if (originalValue is bool originalBool && currentValue is bool currentBool)
        {
            return originalBool == currentBool;
        }

        if (originalValue is IEnumerable<object> originalEnumerable &&
            currentValue is IEnumerable<object> currentEnumerable)
        {
            return AreListsEqual(originalEnumerable, currentEnumerable);
        }

        // For non-nullable types (or if types are not specifically handled above), use Equals
        return originalValue.Equals(currentValue);
    }

    private bool AreListsEqual(IEnumerable<object> list1, IEnumerable<object> list2)
    {
        return list1.SequenceEqual(list2);
    }

    private void CheckForUnsavedChanges()
    {
        var anyChanges = _fieldHistory.Any(entry => !AreValuesEqual(entry.Value.OriginalValue, entry.Value.CurrentValue));
        HasUnsavedChanges = anyChanges;
    }
}
