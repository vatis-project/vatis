// <copyright file="GeneralConfigViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui.Common;
using Vatsim.Vatis.Utils;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Represents the general configuration view model for ATIS configurations.
/// Inherits from <see cref="ReactiveViewModelBase"/>.
/// </summary>
public class GeneralConfigViewModel : ReactiveViewModelBase, IDisposable
{
    private static readonly int[] s_allowedSpeechRates = [120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240];
    private readonly ChangeTracker _changeTracker = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
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

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);

        _changeTracker.HasUnsavedChangesObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(hasUnsavedChanges =>
        {
            HasUnsavedChanges = hasUnsavedChanges;
        }).DisposeWith(_disposables);

        _disposables.Add(AtisStationChanged);

        ProfileSerialNumber = _sessionManager.CurrentProfile?.UpdateSerial != null
            ? $"Profile Serial: {_sessionManager.CurrentProfile.UpdateSerial}"
            : string.Empty;
    }

    /// <summary>
    /// Gets the command triggered when the ATIS station changes.
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
            _changeTracker.TrackChange(nameof(Frequency), value);
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
            _changeTracker.TrackChange(nameof(AtisType), value);
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
            _changeTracker.TrackChange(nameof(CodeRangeLow), value);
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
            _changeTracker.TrackChange(nameof(CodeRangeHigh), value);
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
            _changeTracker.TrackChange(nameof(UseTextToSpeech), value);
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
            _changeTracker.TrackChange(nameof(TextToSpeechVoice), value);
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
            _changeTracker.TrackChange(nameof(UseDecimalTerminology), value);
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
            _changeTracker.TrackChange(nameof(IdsEndpoint), value);
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
            _changeTracker.TrackChange(nameof(SelectedSpeechRate), value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show a duplicate ATIS type error.
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
        _changeTracker.ResetChanges();
    }

    /// <summary>
    /// Applies the general configuration settings for the current session and validates the input data.
    /// </summary>
    /// <param name="hasErrors">A value indicating whether there are errors.</param>
    /// <returns>A boolean value indicating whether the configuration was successfully applied.</returns>
    public bool ApplyConfig(out bool hasErrors)
    {
        if (SelectedStation == null)
        {
            hasErrors = false;
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
                _sessionManager.CurrentProfile.Stations.Any(x =>
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
            hasErrors = true;
            return false;
        }

        if (_sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        hasErrors = false;
        return _changeTracker.ApplyChangesIfNeeded();
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        ClearAllErrors();
        ShowDuplicateAtisTypeError = false;

        _changeTracker.ResetChanges();

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
    }
}
