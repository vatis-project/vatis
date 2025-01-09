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
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly HashSet<string> _initializedProperties = [];

    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    #region Reactive Properties
    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        private set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    private AtisStation? _selectedStation;
    private AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    private string _profileSerialNumber = "";
    public string ProfileSerialNumber
    {
        get => _profileSerialNumber;
        set => this.RaiseAndSetIfChanged(ref _profileSerialNumber, value);
    }
    
    private string? _frequency;
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

    private AtisType _atisType = AtisType.Combined;
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

    private char _codeRangeLow = 'A';
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

    private char _codeRangeHigh = 'Z';
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

    private bool _useTextToSpeech;
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

    private string? _textToSpeechVoice;
    public string? TextToSpeechVoice
    {
        get => _textToSpeechVoice;
        set
        {
            if (_textToSpeechVoice == value || string.IsNullOrEmpty(value)) return;
            this.RaiseAndSetIfChanged(ref _textToSpeechVoice, value);
            if (!_initializedProperties.Add(nameof(TextToSpeechVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _useNotamPrefix;
    public bool UseNotamPrefix
    {
        get => _useNotamPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref _useNotamPrefix, value);
            if (!_initializedProperties.Add(nameof(UseNotamPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _useDecimalTerminology;
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

    private string? _idsEndpoint;
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

    private ObservableCollection<VoiceMetaData>? _availableVoices;
    public ObservableCollection<VoiceMetaData>? AvailableVoices
    {
        get => _availableVoices;
        set => this.RaiseAndSetIfChanged(ref _availableVoices, value);
    }

    private bool _showDuplicateAtisTypeError;
    public bool ShowDuplicateAtisTypeError
    {
        get => _showDuplicateAtisTypeError;
        set => this.RaiseAndSetIfChanged(ref _showDuplicateAtisTypeError, value);
    }
    #endregion

    public GeneralConfigViewModel(ISessionManager sessionManager, IProfileRepository profileRepository)
    {
        _sessionManager = sessionManager;
        _profileRepository = profileRepository;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);
        
        ProfileSerialNumber = mSessionManager.CurrentProfile?.UpdateSerial != null
            ? $"Profile Serial: {mSessionManager.CurrentProfile.UpdateSerial}"
            : string.Empty;
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
            return;

        _initializedProperties.Clear();

        SelectedTabIndex = -1;
        SelectedStation = station;
        Frequency = station.Frequency > 0 ? (station.Frequency / 1000000.0).ToString("000.000", CultureInfo.GetCultureInfo("en-US")) : "";
        AtisType = station.AtisType;
        CodeRangeLow = station.CodeRange.Low;
        CodeRangeHigh = station.CodeRange.High;
        UseDecimalTerminology = station.UseDecimalTerminology;
        UseNotamPrefix = station.UseNotamPrefix;
        IdsEndpoint = station.IdsEndpoint;
        UseTextToSpeech = station.AtisVoice.UseTextToSpeech;
        TextToSpeechVoice = station.AtisVoice.Voice;

        HasUnsavedChanges = false;
    }

    public void Reset()
    {
        Frequency = null;
        AtisType = AtisType.Combined;
        CodeRangeLow = '\0';
        CodeRangeHigh = '\0';
        UseDecimalTerminology = false;
        UseNotamPrefix = false;
        UseTextToSpeech = true;
        IdsEndpoint = null;
        HasUnsavedChanges = false;
    }

    public bool ApplyConfig()
    {
        if (SelectedStation == null)
            return true;

        ClearAllErrors();
        ShowDuplicateAtisTypeError = false;

        if (decimal.TryParse(Frequency, CultureInfo.InvariantCulture, out var frequency))
        {
            frequency = frequency * 1000 * 1000;
            if (frequency is < 118000000 or > 137000000)
            {
                SelectedTabIndex = 0;
                RaiseError(nameof(Frequency),
                    "Invalid frequency format. The accepted frequency range is 118.000-137.000 MHz.");
            }

            if (frequency is >= 0 and <= uint.MaxValue)
            {
                if (frequency != SelectedStation.Frequency)
                    SelectedStation.Frequency = (uint)frequency;
            }
        }
        else
        {
            SelectedTabIndex = 0;
            RaiseError(nameof(Frequency), "Frequency is required.");
        }

        if (SelectedStation.AtisType != AtisType)
        {
            if (_sessionManager.CurrentProfile?.Stations != null && _sessionManager.CurrentProfile.Stations.Any(x =>
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
            SelectedStation.CodeRange = codeRange;

        if (SelectedStation.UseNotamPrefix != UseNotamPrefix)
            SelectedStation.UseNotamPrefix = UseNotamPrefix;

        if (SelectedStation.UseDecimalTerminology != UseDecimalTerminology)
            SelectedStation.UseDecimalTerminology = UseDecimalTerminology;

        if (SelectedStation.IdsEndpoint != IdsEndpoint)
            SelectedStation.IdsEndpoint = IdsEndpoint ?? "";

        if (SelectedStation.AtisVoice.UseTextToSpeech != UseTextToSpeech)
        {
            SelectedStation.AtisVoice.UseTextToSpeech = UseTextToSpeech;
            MessageBus.Current.SendMessage(new AtisVoiceTypeChanged(SelectedStation.Id, UseTextToSpeech));
        }

        if (SelectedStation.AtisVoice.Voice != TextToSpeechVoice)
            SelectedStation.AtisVoice.Voice = TextToSpeechVoice;

        if (HasErrors || ShowDuplicateAtisTypeError)
            return false;

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedChanges = false;
        return true;
    }
}
