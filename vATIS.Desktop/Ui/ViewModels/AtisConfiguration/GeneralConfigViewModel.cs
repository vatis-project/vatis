// <copyright file="GeneralConfigViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Represents the general configuration view model for ATIS configurations.
/// Inherits from <see cref="ReactiveViewModelBase"/>.
/// </summary>
public class GeneralConfigViewModel : ReactiveViewModelBase
{
    private readonly HashSet<string> initializedProperties = [];
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private bool hasUnsavedChanges;
    private int selectedTabIndex;
    private AtisStation? selectedStation;
    private string profileSerialNumber = string.Empty;
    private string? frequency;
    private AtisType atisType = AtisType.Combined;
    private char codeRangeLow = 'A';
    private char codeRangeHigh = 'Z';
    private bool useTextToSpeech;
    private string? textToSpeechVoice;
    private bool useDecimalTerminology;
    private string? idsEndpoint;
    private ObservableCollection<VoiceMetaData>? availableVoices;
    private bool showDuplicateAtisTypeError;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralConfigViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager responsible for managing current session data.</param>
    /// <param name="profileRepository">The profile repository for accessing and managing profile data.</param>
    public GeneralConfigViewModel(ISessionManager sessionManager, IProfileRepository profileRepository)
    {
        this.sessionManager = sessionManager;
        this.profileRepository = profileRepository;

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleUpdateProperties);

        this.ProfileSerialNumber = this.sessionManager.CurrentProfile?.UpdateSerial != null
            ? $"Profile Serial: {this.sessionManager.CurrentProfile.UpdateSerial}"
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
        get => this.hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this.hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets the selected tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => this.selectedTabIndex;
        private set => this.RaiseAndSetIfChanged(ref this.selectedTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => this.selectedStation;
        set => this.RaiseAndSetIfChanged(ref this.selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the profile serial number.
    /// </summary>
    public string ProfileSerialNumber
    {
        get => this.profileSerialNumber;
        set => this.RaiseAndSetIfChanged(ref this.profileSerialNumber, value);
    }

    /// <summary>
    /// Gets or sets the frequency.
    /// </summary>
    public string? Frequency
    {
        get => this.frequency;
        set
        {
            this.RaiseAndSetIfChanged(ref this.frequency, value);
            if (!this.initializedProperties.Add(nameof(this.Frequency)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ATIS type.
    /// </summary>
    public AtisType AtisType
    {
        get => this.atisType;
        set
        {
            this.RaiseAndSetIfChanged(ref this.atisType, value);
            if (!this.initializedProperties.Add(nameof(this.AtisType)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the low range of the code.
    /// </summary>
    public char CodeRangeLow
    {
        get => this.codeRangeLow;
        set
        {
            this.RaiseAndSetIfChanged(ref this.codeRangeLow, value);
            if (!this.initializedProperties.Add(nameof(this.CodeRangeLow)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the high range of the code.
    /// </summary>
    public char CodeRangeHigh
    {
        get => this.codeRangeHigh;
        set
        {
            this.RaiseAndSetIfChanged(ref this.codeRangeHigh, value);
            if (!this.initializedProperties.Add(nameof(this.CodeRangeHigh)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use text-to-speech.
    /// </summary>
    public bool UseTextToSpeech
    {
        get => this.useTextToSpeech;
        set
        {
            this.RaiseAndSetIfChanged(ref this.useTextToSpeech, value);
            if (!this.initializedProperties.Add(nameof(this.UseTextToSpeech)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the text-to-speech voice.
    /// </summary>
    public string? TextToSpeechVoice
    {
        get => this.textToSpeechVoice;
        set
        {
            if (this.textToSpeechVoice == value || string.IsNullOrEmpty(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref this.textToSpeechVoice, value);
            if (!this.initializedProperties.Add(nameof(this.TextToSpeechVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use decimal terminology.
    /// </summary>
    public bool UseDecimalTerminology
    {
        get => this.useDecimalTerminology;
        set
        {
            this.RaiseAndSetIfChanged(ref this.useDecimalTerminology, value);
            if (!this.initializedProperties.Add(nameof(this.UseDecimalTerminology)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the IDS endpoint.
    /// </summary>
    public string? IdsEndpoint
    {
        get => this.idsEndpoint;
        set
        {
            this.RaiseAndSetIfChanged(ref this.idsEndpoint, value);
            if (!this.initializedProperties.Add(nameof(this.IdsEndpoint)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the available voices.
    /// </summary>
    public ObservableCollection<VoiceMetaData>? AvailableVoices
    {
        get => this.availableVoices;
        set => this.RaiseAndSetIfChanged(ref this.availableVoices, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show duplicate ATIS type error.
    /// </summary>
    public bool ShowDuplicateAtisTypeError
    {
        get => this.showDuplicateAtisTypeError;
        set => this.RaiseAndSetIfChanged(ref this.showDuplicateAtisTypeError, value);
    }

    /// <summary>
    /// Resets the state of the <see cref="GeneralConfigViewModel"/> instance to its default configuration.
    /// </summary>
    public void Reset()
    {
        this.Frequency = null;
        this.AtisType = AtisType.Combined;
        this.CodeRangeLow = '\0';
        this.CodeRangeHigh = '\0';
        this.UseDecimalTerminology = false;
        this.UseTextToSpeech = true;
        this.IdsEndpoint = null;
        this.HasUnsavedChanges = false;
    }

    /// <summary>
    /// Applies the general configuration settings for the current session and validates the input data.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the configuration was successfully applied.
    /// </returns>
    public bool ApplyConfig()
    {
        if (this.SelectedStation == null)
        {
            return true;
        }

        this.ClearAllErrors();
        this.ShowDuplicateAtisTypeError = false;

        if (decimal.TryParse(this.Frequency, CultureInfo.InvariantCulture, out var parsedFrequency))
        {
            parsedFrequency = parsedFrequency * 1000 * 1000;
            if (parsedFrequency is < 118000000 or > 137000000)
            {
                this.SelectedTabIndex = 0;
                this.RaiseError(
                    nameof(this.Frequency),
                    "Invalid frequency format. The accepted frequency range is 118.000-137.000 MHz.");
            }

            if (parsedFrequency is >= 0 and <= uint.MaxValue)
            {
                if (parsedFrequency != this.SelectedStation.Frequency)
                {
                    this.SelectedStation.Frequency = (uint)parsedFrequency;
                }
            }
        }
        else
        {
            this.SelectedTabIndex = 0;
            this.RaiseError(nameof(this.Frequency), "Frequency is required.");
        }

        if (this.SelectedStation.AtisType != this.AtisType)
        {
            if (this.sessionManager.CurrentProfile?.Stations != null &&
                this.sessionManager.CurrentProfile.Stations.Any(
                    x =>
                        x != this.SelectedStation && x.Identifier == this.SelectedStation.Identifier &&
                        x.AtisType == this.AtisType))
            {
                this.ShowDuplicateAtisTypeError = true;
            }
            else
            {
                this.SelectedStation.AtisType = this.AtisType;
            }
        }

        if (this.CodeRangeLow == '\0' || this.CodeRangeHigh == '\0')
        {
            this.SelectedTabIndex = 0;
            this.RaiseError(nameof(this.CodeRangeHigh), "ATIS code range is required.");
        }
        else if (this.CodeRangeHigh < this.CodeRangeLow)
        {
            this.SelectedTabIndex = 0;
            this.RaiseError(nameof(this.CodeRangeHigh), "ATIS code range must be in sequential alphabetical order.");
        }

        var codeRange = new CodeRangeMeta(this.CodeRangeLow, this.CodeRangeHigh);

        if (this.SelectedStation.CodeRange != codeRange)
        {
            this.SelectedStation.CodeRange = codeRange;
        }

        if (this.SelectedStation.UseDecimalTerminology != this.UseDecimalTerminology)
        {
            this.SelectedStation.UseDecimalTerminology = this.UseDecimalTerminology;
        }

        if (this.SelectedStation.IdsEndpoint != this.IdsEndpoint)
        {
            this.SelectedStation.IdsEndpoint = this.IdsEndpoint ?? string.Empty;
        }

        if (this.SelectedStation.AtisVoice.UseTextToSpeech != this.UseTextToSpeech)
        {
            this.SelectedStation.AtisVoice.UseTextToSpeech = this.UseTextToSpeech;
            MessageBus.Current.SendMessage(new AtisVoiceTypeChanged(this.SelectedStation.Id, this.UseTextToSpeech));
        }

        if (this.SelectedStation.AtisVoice.Voice != this.TextToSpeechVoice)
        {
            this.SelectedStation.AtisVoice.Voice = this.TextToSpeechVoice;
        }

        if (this.HasErrors || this.ShowDuplicateAtisTypeError)
        {
            return false;
        }

        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
        }

        this.HasUnsavedChanges = false;
        return true;
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.initializedProperties.Clear();

        this.SelectedTabIndex = -1;
        this.SelectedStation = station;
        this.Frequency = station.Frequency > 0
            ? (station.Frequency / 1000000.0).ToString("000.000", CultureInfo.GetCultureInfo("en-US"))
            : string.Empty;
        this.AtisType = station.AtisType;
        this.CodeRangeLow = station.CodeRange.Low;
        this.CodeRangeHigh = station.CodeRange.High;
        this.UseDecimalTerminology = station.UseDecimalTerminology;
        this.IdsEndpoint = station.IdsEndpoint;
        this.UseTextToSpeech = station.AtisVoice.UseTextToSpeech;
        this.TextToSpeechVoice = station.AtisVoice.Voice;

        this.HasUnsavedChanges = false;
    }
}
