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

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Represents the general configuration view model for ATIS configurations.
/// Inherits from <see cref="ReactiveViewModelBase"/>.
/// </summary>
public class GeneralConfigViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly HashSet<string> _initializedProperties = [];
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
            if (!_initializedProperties.Add(nameof(Frequency)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(AtisType)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(CodeRangeLow)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(CodeRangeHigh)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(UseTextToSpeech)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(TextToSpeechVoice)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(UseDecimalTerminology)))
            {
                HasUnsavedChanges = true;
            }
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
            if (!_initializedProperties.Add(nameof(IdsEndpoint)))
            {
                HasUnsavedChanges = true;
            }
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
            return true;
        }

        ClearAllErrors();
        ShowDuplicateAtisTypeError = false;

        if (decimal.TryParse(Frequency, CultureInfo.InvariantCulture, out var parsedFrequency))
        {
            parsedFrequency = parsedFrequency * 1000 * 1000;
            if (parsedFrequency is < 118000000 or > 137000000)
            {
                SelectedTabIndex = 0;
                RaiseError(
                    nameof(Frequency),
                    "Invalid frequency format. The accepted frequency range is 118.000-137.000 MHz.");
            }

            if (parsedFrequency is >= 0 and <= uint.MaxValue)
            {
                if (parsedFrequency != SelectedStation.Frequency)
                {
                    SelectedStation.Frequency = (uint)parsedFrequency;
                }
            }
        }
        else
        {
            SelectedTabIndex = 0;
            RaiseError(nameof(Frequency), "Frequency is required.");
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

        if (HasErrors || ShowDuplicateAtisTypeError)
        {
            return false;
        }

        if (_sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        HasUnsavedChanges = false;
        return true;
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        _initializedProperties.Clear();

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

        HasUnsavedChanges = false;
    }
}
