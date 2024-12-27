using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

public class GeneralConfigViewModel : ReactiveViewModelBase
{
    private readonly IProfileRepository mProfileRepository;
    private readonly ISessionManager mSessionManager;
    private readonly IAppConfig mAppConfig;
    private readonly HashSet<string> mInitializedProperties = [];
    
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    
    #region Reactive Properties
    private bool mHasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => mHasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref mHasUnsavedChanges, value);
    }

    private int mSelectedTabIndex;
    public int SelectedTabIndex
    {
        get => mSelectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref mSelectedTabIndex, value);
    }
    
    private AtisStation? mSelectedStation;
    public AtisStation? SelectedStation
    {
        get => mSelectedStation;
        set => this.RaiseAndSetIfChanged(ref mSelectedStation, value);
    }
    
    private string? mFrequency;
    public string? Frequency
    {
        get => mFrequency;
        set
        {
            this.RaiseAndSetIfChanged(ref mFrequency, value);
            if (!mInitializedProperties.Add(nameof(Frequency)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private AtisType mAtisType = AtisType.Combined;
    public AtisType AtisType
    {
        get => mAtisType;
        set
        {
            this.RaiseAndSetIfChanged(ref mAtisType, value);
            if (!mInitializedProperties.Add(nameof(AtisType)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private char mCodeRangeLow = 'A';
    public char CodeRangeLow
    {
        get => mCodeRangeLow;
        set
        {
            this.RaiseAndSetIfChanged(ref mCodeRangeLow, value);
            if (!mInitializedProperties.Add(nameof(CodeRangeLow)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private char mCodeRangeHigh = 'Z';
    public char CodeRangeHigh
    {
        get => mCodeRangeHigh;
        set
        {
            this.RaiseAndSetIfChanged(ref mCodeRangeHigh, value);
            if (!mInitializedProperties.Add(nameof(CodeRangeHigh)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mUseTextToSpeech;
    public bool UseTextToSpeech
    {
        get => mUseTextToSpeech;
        set
        {
            this.RaiseAndSetIfChanged(ref mUseTextToSpeech, value);
            if (!mInitializedProperties.Add(nameof(UseTextToSpeech)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mTextToSpeechVoice;
    public string? TextToSpeechVoice
    {
        get => mTextToSpeechVoice;
        set
        {
            if (mTextToSpeechVoice == value || string.IsNullOrEmpty(value)) return;
            this.RaiseAndSetIfChanged(ref mTextToSpeechVoice, value);
            if (!mInitializedProperties.Add(nameof(TextToSpeechVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mUseNotamPrefix;
    public bool UseNotamPrefix
    {
        get => mUseNotamPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref mUseNotamPrefix, value);
            if (!mInitializedProperties.Add(nameof(UseNotamPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mUseDecimalTerminology;
    public bool UseDecimalTerminology
    {
        get => mUseDecimalTerminology;
        set
        {
            this.RaiseAndSetIfChanged(ref mUseDecimalTerminology, value);
            if (!mInitializedProperties.Add(nameof(UseDecimalTerminology)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mIdsEndpoint;
    public string? IdsEndpoint
    {
        get => mIdsEndpoint;
        set
        {
            this.RaiseAndSetIfChanged(ref mIdsEndpoint, value);
            if (!mInitializedProperties.Add(nameof(IdsEndpoint)))
            {
                HasUnsavedChanges = true;
            }
        }
    }
    
    private ObservableCollection<VoiceMetaData>? mAvailableVoices;
    public ObservableCollection<VoiceMetaData>? AvailableVoices
    {
        get => mAvailableVoices;
        set => this.RaiseAndSetIfChanged(ref mAvailableVoices, value);
    }
    
    private bool mShowDuplicateAtisTypeError;
    public bool ShowDuplicateAtisTypeError
    {
        get => mShowDuplicateAtisTypeError;
        set => this.RaiseAndSetIfChanged(ref mShowDuplicateAtisTypeError, value);
    }
    #endregion

    public GeneralConfigViewModel(IAppConfig appConfig, ISessionManager sessionManager, IProfileRepository profileRepository)
    {
        mAppConfig = appConfig;
        mSessionManager = sessionManager;
        mProfileRepository = profileRepository;
        
        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
            return;

        mInitializedProperties.Clear();
        
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
            if (mSessionManager.CurrentProfile?.Stations != null && mSessionManager.CurrentProfile.Stations.Any(x =>
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

        if (mSessionManager.CurrentProfile != null)
            mProfileRepository.Save(mSessionManager.CurrentProfile);

        HasUnsavedChanges = false;
        return true;
    }
}