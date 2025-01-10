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

public class GeneralConfigViewModel : ReactiveViewModelBase
{
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;

    public GeneralConfigViewModel(ISessionManager sessionManager, IProfileRepository profileRepository)
    {
        this._sessionManager = sessionManager;
        this._profileRepository = profileRepository;

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleUpdateProperties);

        this.ProfileSerialNumber = this._sessionManager.CurrentProfile?.UpdateSerial != null
            ? $"Profile Serial: {this._sessionManager.CurrentProfile.UpdateSerial}"
            : string.Empty;
    }

    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this._initializedProperties.Clear();

        this.SelectedTabIndex = -1;
        this.SelectedStation = station;
        this.Frequency = station.Frequency > 0
            ? (station.Frequency / 1000000.0).ToString("000.000", CultureInfo.GetCultureInfo("en-US"))
            : "";
        this.AtisType = station.AtisType;
        this.CodeRangeLow = station.CodeRange.Low;
        this.CodeRangeHigh = station.CodeRange.High;
        this.UseDecimalTerminology = station.UseDecimalTerminology;
        this.IdsEndpoint = station.IdsEndpoint;
        this.UseTextToSpeech = station.AtisVoice.UseTextToSpeech;
        this.TextToSpeechVoice = station.AtisVoice.Voice;

        this.HasUnsavedChanges = false;
    }

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

    public bool ApplyConfig()
    {
        if (this.SelectedStation == null)
        {
            return true;
        }

        this.ClearAllErrors();
        this.ShowDuplicateAtisTypeError = false;

        if (decimal.TryParse(this.Frequency, CultureInfo.InvariantCulture, out var frequency))
        {
            frequency = frequency * 1000 * 1000;
            if (frequency is < 118000000 or > 137000000)
            {
                this.SelectedTabIndex = 0;
                this.RaiseError(
                    nameof(this.Frequency),
                    "Invalid frequency format. The accepted frequency range is 118.000-137.000 MHz.");
            }

            if (frequency is >= 0 and <= uint.MaxValue)
            {
                if (frequency != this.SelectedStation.Frequency)
                {
                    this.SelectedStation.Frequency = (uint)frequency;
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
            if (this._sessionManager.CurrentProfile?.Stations != null &&
                this._sessionManager.CurrentProfile.Stations.Any(
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
            this.SelectedStation.IdsEndpoint = this.IdsEndpoint ?? "";
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

        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedChanges = false;
        return true;
    }

    #region Reactive Properties

    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => this._hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this._hasUnsavedChanges, value);
    }

    private int _selectedTabIndex;

    public int SelectedTabIndex
    {
        get => this._selectedTabIndex;
        private set => this.RaiseAndSetIfChanged(ref this._selectedTabIndex, value);
    }

    private AtisStation? _selectedStation;

    private AtisStation? SelectedStation
    {
        get => this._selectedStation;
        set => this.RaiseAndSetIfChanged(ref this._selectedStation, value);
    }

    private string _profileSerialNumber = "";

    public string ProfileSerialNumber
    {
        get => this._profileSerialNumber;
        set => this.RaiseAndSetIfChanged(ref this._profileSerialNumber, value);
    }

    private string? _frequency;

    public string? Frequency
    {
        get => this._frequency;
        set
        {
            this.RaiseAndSetIfChanged(ref this._frequency, value);
            if (!this._initializedProperties.Add(nameof(this.Frequency)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private AtisType _atisType = AtisType.Combined;

    public AtisType AtisType
    {
        get => this._atisType;
        set
        {
            this.RaiseAndSetIfChanged(ref this._atisType, value);
            if (!this._initializedProperties.Add(nameof(this.AtisType)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private char _codeRangeLow = 'A';

    public char CodeRangeLow
    {
        get => this._codeRangeLow;
        set
        {
            this.RaiseAndSetIfChanged(ref this._codeRangeLow, value);
            if (!this._initializedProperties.Add(nameof(this.CodeRangeLow)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private char _codeRangeHigh = 'Z';

    public char CodeRangeHigh
    {
        get => this._codeRangeHigh;
        set
        {
            this.RaiseAndSetIfChanged(ref this._codeRangeHigh, value);
            if (!this._initializedProperties.Add(nameof(this.CodeRangeHigh)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _useTextToSpeech;

    public bool UseTextToSpeech
    {
        get => this._useTextToSpeech;
        set
        {
            this.RaiseAndSetIfChanged(ref this._useTextToSpeech, value);
            if (!this._initializedProperties.Add(nameof(this.UseTextToSpeech)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _textToSpeechVoice;

    public string? TextToSpeechVoice
    {
        get => this._textToSpeechVoice;
        set
        {
            if (this._textToSpeechVoice == value || string.IsNullOrEmpty(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref this._textToSpeechVoice, value);
            if (!this._initializedProperties.Add(nameof(this.TextToSpeechVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _useDecimalTerminology;

    public bool UseDecimalTerminology
    {
        get => this._useDecimalTerminology;
        set
        {
            this.RaiseAndSetIfChanged(ref this._useDecimalTerminology, value);
            if (!this._initializedProperties.Add(nameof(this.UseDecimalTerminology)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _idsEndpoint;

    public string? IdsEndpoint
    {
        get => this._idsEndpoint;
        set
        {
            this.RaiseAndSetIfChanged(ref this._idsEndpoint, value);
            if (!this._initializedProperties.Add(nameof(this.IdsEndpoint)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private ObservableCollection<VoiceMetaData>? _availableVoices;

    public ObservableCollection<VoiceMetaData>? AvailableVoices
    {
        get => this._availableVoices;
        set => this.RaiseAndSetIfChanged(ref this._availableVoices, value);
    }

    private bool _showDuplicateAtisTypeError;

    public bool ShowDuplicateAtisTypeError
    {
        get => this._showDuplicateAtisTypeError;
        set => this.RaiseAndSetIfChanged(ref this._showDuplicateAtisTypeError, value);
    }

    #endregion
}