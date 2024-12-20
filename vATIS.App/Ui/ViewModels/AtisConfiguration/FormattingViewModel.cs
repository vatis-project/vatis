using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using ReactiveUI;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Controls;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

public class FormattingViewModel : ReactiveViewModelBase
{
    private readonly IWindowFactory mWindowFactory;
    private readonly IProfileRepository mProfileRepository;
    private readonly ISessionManager mSessionManager;
    private readonly HashSet<string> mInitializedProperties = [];
    
    public IDialogOwner? DialogOwner { get; set; }
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    public ReactiveCommand<string, Unit> TemplateVariableClicked { get;  }
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }
    public ReactiveCommand<Unit, Unit> AddTransitionLevelCommand { get;  }
    public ReactiveCommand<TransitionLevelMeta, Unit> DeleteTransitionLevelCommand { get;  }

    #region UI Properties
    private ObservableCollection<string>? mFormattingOptions;
    public ObservableCollection<string>? FormattingOptions
    {
        get => mFormattingOptions;
        set => this.RaiseAndSetIfChanged(ref mFormattingOptions, value);
    }
    
    private AtisStation? mSelectedStation;
    public AtisStation? SelectedStation
    {
        get => mSelectedStation;
        set => this.RaiseAndSetIfChanged(ref mSelectedStation, value);
    }

    private bool mHasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => mHasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref mHasUnsavedChanges, value);
    }
    
    private string? mSelectedFormattingOption;
    public string? SelectedFormattingOption
    {
        get => mSelectedFormattingOption;
        set => this.RaiseAndSetIfChanged(ref mSelectedFormattingOption, value);
    }
    #endregion

    #region Config Properties
     private string? mRoutineObservationTime;
    public string? RoutineObservationTime
    {
        get => mRoutineObservationTime;
        set
        {
            this.RaiseAndSetIfChanged(ref mRoutineObservationTime, value);
            if (!mInitializedProperties.Add(nameof(RoutineObservationTime)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mObservationTimeTextTemplate;
    public string? ObservationTimeTextTemplate
    {
        get => mObservationTimeTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mObservationTimeTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(ObservationTimeTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mObservationTimeVoiceTemplate;
    public string? ObservationTimeVoiceTemplate
    {
        get => mObservationTimeVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mObservationTimeVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(ObservationTimeVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mSpeakWindSpeedLeadingZero;
    public bool SpeakWindSpeedLeadingZero
    {
        get => mSpeakWindSpeedLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref mSpeakWindSpeedLeadingZero, value);
            if (!mInitializedProperties.Add(nameof(SpeakWindSpeedLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mMagneticVariationEnabled;
    public bool MagneticVariationEnabled
    {
        get => mMagneticVariationEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref mMagneticVariationEnabled, value);
            if (!mInitializedProperties.Add(nameof(MagneticVariationEnabled)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mMagneticVariationValue;
    public string? MagneticVariationValue
    {
        get => mMagneticVariationValue;
        set
        {
            this.RaiseAndSetIfChanged(ref mMagneticVariationValue, value);
            if (!mInitializedProperties.Add(nameof(MagneticVariationValue)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mStandardWindTextTemplate;
    public string? StandardWindTextTemplate
    {
        get => mStandardWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mStandardWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(StandardWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mStandardWindVoiceTemplate;
    public string? StandardWindVoiceTemplate
    {
        get => mStandardWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mStandardWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(StandardWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mStandardGustWindTextTemplate;
    public string? StandardGustWindTextTemplate
    {
        get => mStandardGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mStandardGustWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(StandardGustWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mStandardGustWindVoiceTemplate;
    public string? StandardGustWindVoiceTemplate
    {
        get => mStandardGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mStandardGustWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(StandardGustWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableWindTextTemplate;
    public string? VariableWindTextTemplate
    {
        get => mVariableWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableWindVoiceTemplate;
    public string? VariableWindVoiceTemplate
    {
        get => mVariableWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableGustWindTextTemplate;
    public string? VariableGustWindTextTemplate
    {
        get => mVariableGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableGustWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableGustWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableGustWindVoiceTemplate;
    public string? VariableGustWindVoiceTemplate
    {
        get => mVariableGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableGustWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableGustWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableDirectionWindTextTemplate;
    public string? VariableDirectionWindTextTemplate
    {
        get => mVariableDirectionWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableDirectionWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableDirectionWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVariableDirectionWindVoiceTemplate;
    public string? VariableDirectionWindVoiceTemplate
    {
        get => mVariableDirectionWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVariableDirectionWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(VariableDirectionWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mCalmWindTextTemplate;
    public string? CalmWindTextTemplate
    {
        get => mCalmWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mCalmWindTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(CalmWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mCalmWindVoiceTemplate;
    public string? CalmWindVoiceTemplate
    {
        get => mCalmWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mCalmWindVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(CalmWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mCalmWindSpeed;
    public string? CalmWindSpeed
    {
        get => mCalmWindSpeed;
        set
        {
            this.RaiseAndSetIfChanged(ref mCalmWindSpeed, value);
            if (!mInitializedProperties.Add(nameof(CalmWindSpeed)))
            {
                HasUnsavedChanges = true;
            }
        }
    }
    
    private string? mVisibilityTextTemplate;
    public string? VisibilityTextTemplate
    {
        get => mVisibilityTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(VisibilityTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityVoiceTemplate;
    public string? VisibilityVoiceTemplate
    {
        get => mVisibilityVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(VisibilityVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherTextTemplate;
    public string? PresentWeatherTextTemplate
    {
        get => mPresentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherVoiceTemplate;
    public string? PresentWeatherVoiceTemplate
    {
        get => mPresentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mCloudsTextTemplate;
    public string? CloudsTextTemplate
    {
        get => mCloudsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mCloudsTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(CloudsTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mCloudsVoiceTemplate;
    public string? CloudsVoiceTemplate
    {
        get => mCloudsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mCloudsVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(CloudsVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mTemperatureTextTemplate;
    public string? TemperatureTextTemplate
    {
        get => mTemperatureTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mTemperatureTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(TemperatureTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mTemperatureVoiceTemplate;
    public string? TemperatureVoiceTemplate
    {
        get => mTemperatureVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mTemperatureVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(TemperatureVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mDewpointTextTemplate;
    public string? DewpointTextTemplate
    {
        get => mDewpointTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mDewpointTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(DewpointTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mDewpointVoiceTemplate;
    public string? DewpointVoiceTemplate
    {
        get => mDewpointVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mDewpointVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(DewpointVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mAltimeterTextTemplate;
    public string? AltimeterTextTemplate
    {
        get => mAltimeterTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mAltimeterTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(AltimeterTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mAltimeterVoiceTemplate;
    public string? AltimeterVoiceTemplate
    {
        get => mAltimeterVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mAltimeterVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(AltimeterVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mClosingStatementTextTemplate;
    public string? ClosingStatementTextTemplate
    {
        get => mClosingStatementTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mClosingStatementTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(ClosingStatementTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mClosingStatementVoiceTemplate;
    public string? ClosingStatementVoiceTemplate
    {
        get => mClosingStatementVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mClosingStatementVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(ClosingStatementVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityNorth;
    public string? VisibilityNorth
    {
        get => mVisibilityNorth;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityNorth, value);
            if (!mInitializedProperties.Add(nameof(VisibilityNorth)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityNorthEast;
    public string? VisibilityNorthEast
    {
        get => mVisibilityNorthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityNorthEast, value);
            if (!mInitializedProperties.Add(nameof(VisibilityNorthEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityEast;
    public string? VisibilityEast
    {
        get => mVisibilityEast;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityEast, value);
            if (!mInitializedProperties.Add(nameof(VisibilityEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilitySouthEast;
    public string? VisibilitySouthEast
    {
        get => mVisibilitySouthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilitySouthEast, value);
            if (!mInitializedProperties.Add(nameof(VisibilitySouthEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilitySouth;
    public string? VisibilitySouth
    {
        get => mVisibilitySouth;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilitySouth, value);
            if (!mInitializedProperties.Add(nameof(VisibilitySouth)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilitySouthWest;
    public string? VisibilitySouthWest
    {
        get => mVisibilitySouthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilitySouthWest, value);
            if (!mInitializedProperties.Add(nameof(VisibilitySouthWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityWest;
    public string? VisibilityWest
    {
        get => mVisibilityWest;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityWest, value);
            if (!mInitializedProperties.Add(nameof(VisibilityWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityNorthWest;
    public string? VisibilityNorthWest
    {
        get => mVisibilityNorthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityNorthWest, value);
            if (!mInitializedProperties.Add(nameof(VisibilityNorthWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityUnlimitedVisibilityVoice;
    public string? VisibilityUnlimitedVisibilityVoice
    {
        get => mVisibilityUnlimitedVisibilityVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityUnlimitedVisibilityVoice, value);
            if (!mInitializedProperties.Add(nameof(VisibilityUnlimitedVisibilityVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mVisibilityUnlimitedVisibilityText;
    public string? VisibilityUnlimitedVisibilityText
    {
        get => mVisibilityUnlimitedVisibilityText;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityUnlimitedVisibilityText, value);
            if (!mInitializedProperties.Add(nameof(VisibilityUnlimitedVisibilityText)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mVisibilityIncludeVisibilitySuffix;
    public bool VisibilityIncludeVisibilitySuffix
    {
        get => mVisibilityIncludeVisibilitySuffix;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityIncludeVisibilitySuffix, value);
            if (!mInitializedProperties.Add(nameof(VisibilityIncludeVisibilitySuffix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private int mVisibilityMetersCutoff;
    public int VisibilityMetersCutoff
    {
        get => mVisibilityMetersCutoff;
        set
        {
            this.RaiseAndSetIfChanged(ref mVisibilityMetersCutoff, value);
            if (!mInitializedProperties.Add(nameof(VisibilityMetersCutoff)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherLightIntensity;
    public string? PresentWeatherLightIntensity
    {
        get => mPresentWeatherLightIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherLightIntensity, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherLightIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherModerateIntensity;
    public string? PresentWeatherModerateIntensity
    {
        get => mPresentWeatherModerateIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherModerateIntensity, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherModerateIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherHeavyIntensity;
    public string? PresentWeatherHeavyIntensity
    {
        get => mPresentWeatherHeavyIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherHeavyIntensity, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherHeavyIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mPresentWeatherVicinity;
    public string? PresentWeatherVicinity
    {
        get => mPresentWeatherVicinity;
        set
        {
            this.RaiseAndSetIfChanged(ref mPresentWeatherVicinity, value);
            if (!mInitializedProperties.Add(nameof(PresentWeatherVicinity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mCloudsIdentifyCeilingLayer;
    public bool CloudsIdentifyCeilingLayer
    {
        get => mCloudsIdentifyCeilingLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref mCloudsIdentifyCeilingLayer, value);
            if (!mInitializedProperties.Add(nameof(CloudsIdentifyCeilingLayer)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mCloudsConvertToMetric;
    public bool CloudsConvertToMetric
    {
        get => mCloudsConvertToMetric;
        set
        {
            this.RaiseAndSetIfChanged(ref mCloudsConvertToMetric, value);
            if (!mInitializedProperties.Add(nameof(CloudsConvertToMetric)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mUndeterminedLayerAltitudeText;
    public string? UndeterminedLayerAltitudeText
    {
        get => mUndeterminedLayerAltitudeText;
        set
        {
            this.RaiseAndSetIfChanged(ref mUndeterminedLayerAltitudeText, value);
            if (!mInitializedProperties.Add(nameof(UndeterminedLayerAltitudeText)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mUndeterminedLayerAltitudeVoice;
    public string? UndeterminedLayerAltitudeVoice
    {
        get => mUndeterminedLayerAltitudeVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref mUndeterminedLayerAltitudeVoice, value);
            if (!mInitializedProperties.Add(nameof(UndeterminedLayerAltitudeVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mCloudHeightAltitudeInHundreds;
    public bool CloudHeightAltitudeInHundreds
    {
        get => mCloudHeightAltitudeInHundreds;
        set
        {
            this.RaiseAndSetIfChanged(ref mCloudHeightAltitudeInHundreds, value);
            if (!mInitializedProperties.Add(nameof(CloudHeightAltitudeInHundreds)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mTemperatureUsePlusPrefix;
    public bool TemperatureUsePlusPrefix
    {
        get => mTemperatureUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref mTemperatureUsePlusPrefix, value);
            if (!mInitializedProperties.Add(nameof(TemperatureUsePlusPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mTemperatureSpeakLeadingZero;
    public bool TemperatureSpeakLeadingZero
    {
        get => mTemperatureSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref mTemperatureSpeakLeadingZero, value);
            if (!mInitializedProperties.Add(nameof(TemperatureSpeakLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mDewpointUsePlusPrefix;
    public bool DewpointUsePlusPrefix
    {
        get => mDewpointUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref mDewpointUsePlusPrefix, value);
            if (!mInitializedProperties.Add(nameof(DewpointUsePlusPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mDewpointSpeakLeadingZero;
    public bool DewpointSpeakLeadingZero
    {
        get => mDewpointSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref mDewpointSpeakLeadingZero, value);
            if (!mInitializedProperties.Add(nameof(DewpointSpeakLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mAltimeterSpeakDecimal;
    private bool AltimeterSpeakDecimal
    {
        get => mAltimeterSpeakDecimal;
        set
        {
            this.RaiseAndSetIfChanged(ref mAltimeterSpeakDecimal, value);
            if (!mInitializedProperties.Add(nameof(AltimeterSpeakDecimal)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool mClosingStatementAutoIncludeClosingStatement;
    public bool ClosingStatementAutoIncludeClosingStatement
    {
        get => mClosingStatementAutoIncludeClosingStatement;
        set
        {
            this.RaiseAndSetIfChanged(ref mClosingStatementAutoIncludeClosingStatement, value);
            if (!mInitializedProperties.Add(nameof(ClosingStatementAutoIncludeClosingStatement)))
            {
                HasUnsavedChanges = true;
            }
        }
    }
    

    private ObservableCollection<TransitionLevelMeta>? mTransitionLevels;
    public ObservableCollection<TransitionLevelMeta>? TransitionLevels
    {
        get => mTransitionLevels;
        set
        {
            this.RaiseAndSetIfChanged(ref mTransitionLevels, value);
            if (!mInitializedProperties.Add(nameof(TransitionLevels)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mTransitionLevelTextTemplate;
    public string? TransitionLevelTextTemplate
    {
        get => mTransitionLevelTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mTransitionLevelTextTemplate, value);
            if (!mInitializedProperties.Add(nameof(TransitionLevelTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? mTransitionLevelVoiceTemplate;
    public string? TransitionLevelVoiceTemplate
    {
        get => mTransitionLevelVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref mTransitionLevelVoiceTemplate, value);
            if (!mInitializedProperties.Add(nameof(TransitionLevelVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }
    
    private ObservableCollection<PresentWeatherMeta>? mPresentWeatherTypes;
    public ObservableCollection<PresentWeatherMeta>? PresentWeatherTypes
    {
        get => mPresentWeatherTypes;
        set => this.RaiseAndSetIfChanged(ref mPresentWeatherTypes, value);
    }

    private ObservableCollection<CloudTypeMeta>? mCloudTypes;
    public ObservableCollection<CloudTypeMeta>? CloudTypes
    {
        get => mCloudTypes;
        set => this.RaiseAndSetIfChanged(ref mCloudTypes, value);
    }
    
    private ObservableCollection<ConvectiveCloudTypeMeta>? mConvectiveCloudTypes;
    public ObservableCollection<ConvectiveCloudTypeMeta>? ConvectiveCloudTypes
    {
        get => mConvectiveCloudTypes;
        set => this.RaiseAndSetIfChanged(ref mConvectiveCloudTypes, value);
    }
    
    private List<ICompletionData> mContractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => mContractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref mContractionCompletionData, value);
    }
    #endregion
    
    public FormattingViewModel(IWindowFactory windowFactory, IProfileRepository profileRepository, ISessionManager sessionManager)
    {
        mWindowFactory = windowFactory;
        mProfileRepository = profileRepository;
        mSessionManager = sessionManager;

        FormattingOptions = [];
        
        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        TemplateVariableClicked = ReactiveCommand.Create<string>(HandleTemplateVariableClicked);
        CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(HandleCellEditEnding);
        AddTransitionLevelCommand = ReactiveCommand.CreateFromTask(HandleAddTransitionLevel);
        DeleteTransitionLevelCommand = ReactiveCommand.CreateFromTask<TransitionLevelMeta>(HandleDeleteTransitionLevel);
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
            return;

        SelectedStation = station;
        
        FormattingOptions = [];
        if (station.IsFaaAtis)
        {
            FormattingOptions =
            [
                "Observation Time",
                "Wind",
                "Visibility",
                "Weather",
                "Clouds",
                "Temperature",
                "Dewpoint",
                "Altimeter",
                "Closing Statement"
            ];
        }
        else
        {
            FormattingOptions =
            [
                "Observation Time",
                "Wind",
                "Visibility",
                "Weather",
                "Clouds",
                "Temperature",
                "Dewpoint",
                "Altimeter",
                "Transition Level",
                "Closing Statement"
            ];
        }
        
        SpeakWindSpeedLeadingZero = station.AtisFormat.SurfaceWind.SpeakLeadingZero;
        MagneticVariationEnabled = station.AtisFormat.SurfaceWind.MagneticVariation.Enabled;
        MagneticVariationValue = station.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees.ToString();
        RoutineObservationTime = string.Join(",", station.AtisFormat.ObservationTime.StandardUpdateTime ?? []);
        ObservationTimeTextTemplate = station.AtisFormat.ObservationTime.Template.Text;
        ObservationTimeVoiceTemplate = station.AtisFormat.ObservationTime.Template.Voice;
        StandardWindTextTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Text;
        StandardWindVoiceTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Voice;
        StandardGustWindTextTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Text;
        StandardGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Voice;
        VariableWindTextTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Text;
        VariableWindVoiceTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Voice;
        VariableGustWindTextTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Text;
        VariableGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Voice;
        VariableDirectionWindTextTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Text;
        VariableDirectionWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Voice;
        CalmWindTextTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Text;
        CalmWindVoiceTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Voice;
        CalmWindSpeed = station.AtisFormat.SurfaceWind.Calm.CalmWindSpeed.ToString();
        VisibilityTextTemplate = station.AtisFormat.Visibility.Template.Text;
        VisibilityVoiceTemplate = station.AtisFormat.Visibility.Template.Voice;
        PresentWeatherTextTemplate = station.AtisFormat.PresentWeather.Template.Text;
        PresentWeatherVoiceTemplate = station.AtisFormat.PresentWeather.Template.Voice;
        CloudsTextTemplate = station.AtisFormat.Clouds.Template.Text;
        CloudsVoiceTemplate = station.AtisFormat.Clouds.Template.Voice;
        TemperatureTextTemplate = station.AtisFormat.Temperature.Template.Text;
        TemperatureVoiceTemplate = station.AtisFormat.Temperature.Template.Voice;
        DewpointTextTemplate = station.AtisFormat.Dewpoint.Template.Text;
        DewpointVoiceTemplate = station.AtisFormat.Dewpoint.Template.Voice;
        AltimeterTextTemplate = station.AtisFormat.Altimeter.Template.Text;
        AltimeterVoiceTemplate = station.AtisFormat.Altimeter.Template.Voice;
        ClosingStatementTextTemplate = station.AtisFormat.ClosingStatement.Template.Text;
        ClosingStatementVoiceTemplate = station.AtisFormat.ClosingStatement.Template.Voice;
        VisibilityNorth = station.AtisFormat.Visibility.North;
        VisibilityNorthEast = station.AtisFormat.Visibility.NorthEast;
        VisibilityEast = station.AtisFormat.Visibility.East;
        VisibilitySouthEast = station.AtisFormat.Visibility.SouthEast;
        VisibilitySouth = station.AtisFormat.Visibility.South;
        VisibilitySouthWest = station.AtisFormat.Visibility.SouthWest;
        VisibilityWest = station.AtisFormat.Visibility.West;
        VisibilityNorthWest = station.AtisFormat.Visibility.NorthWest;
        VisibilityUnlimitedVisibilityVoice = station.AtisFormat.Visibility.UnlimitedVisibilityVoice;
        VisibilityUnlimitedVisibilityText = station.AtisFormat.Visibility.UnlimitedVisibilityText;
        VisibilityIncludeVisibilitySuffix = station.AtisFormat.Visibility.IncludeVisibilitySuffix;
        VisibilityMetersCutoff = station.AtisFormat.Visibility.MetersCutoff;
        PresentWeatherLightIntensity = station.AtisFormat.PresentWeather.LightIntensity;
        PresentWeatherModerateIntensity = station.AtisFormat.PresentWeather.ModerateIntensity;
        PresentWeatherHeavyIntensity = station.AtisFormat.PresentWeather.HeavyIntensity;
        PresentWeatherVicinity = station.AtisFormat.PresentWeather.Vicinity;
        CloudsIdentifyCeilingLayer = station.AtisFormat.Clouds.IdentifyCeilingLayer;
        CloudsConvertToMetric = station.AtisFormat.Clouds.ConvertToMetric;
        CloudHeightAltitudeInHundreds = station.AtisFormat.Clouds.IsAltitudeInHundreds;
        UndeterminedLayerAltitudeText = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Text;
        UndeterminedLayerAltitudeVoice = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice;
        TemperatureUsePlusPrefix = station.AtisFormat.Temperature.UsePlusPrefix;
        TemperatureSpeakLeadingZero = station.AtisFormat.Temperature.SpeakLeadingZero;
        DewpointUsePlusPrefix = station.AtisFormat.Dewpoint.UsePlusPrefix;
        DewpointSpeakLeadingZero = station.AtisFormat.Dewpoint.SpeakLeadingZero;
        AltimeterSpeakDecimal = station.AtisFormat.Altimeter.PronounceDecimal;
        ClosingStatementAutoIncludeClosingStatement = station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement;
        
        PresentWeatherTypes = [];
        foreach (var kvp in station.AtisFormat.PresentWeather.PresentWeatherTypes)
        {
            PresentWeatherTypes.Add(new PresentWeatherMeta(kvp.Key, kvp.Value.Text, kvp.Value.Spoken));
        }
        
        CloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.Types)
        {
            CloudTypes.Add(new CloudTypeMeta(item.Key, item.Value.Voice, item.Value.Text));
        }
        
        ConvectiveCloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.ConvectiveTypes)
        {
            ConvectiveCloudTypes.Add(new ConvectiveCloudTypeMeta(item.Key, item.Value));
        }
        
        TransitionLevels = [];
        foreach (var item in station.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low))
        {
            TransitionLevels.Add(item);
        }
        
        ContractionCompletionData = [];
        foreach (var contraction in station.Contractions)
        {
            if (!string.IsNullOrEmpty(contraction.VariableName) && !string.IsNullOrEmpty(contraction.Voice))
            {
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }

        HasUnsavedChanges = false;
    }

    private async Task HandleAddTransitionLevel()
    {
        if (DialogOwner == null || SelectedStation == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            string? previousQnhLow = null;
            string? previousQnhHigh = null;
            string? previousTransitionLevel = null;

            var dialog = mWindowFactory.CreateTransitionLevelDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is TransitionLevelDialogViewModel context)
            {
                context.QnhLow = previousQnhLow;
                context.QnhHigh = previousQnhHigh;
                context.TransitionLevel = previousTransitionLevel;

                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearAllErrors();
                        
                        previousQnhLow = context.QnhLow;
                        previousQnhHigh = context.QnhHigh;
                        previousTransitionLevel = context.TransitionLevel;

                        if (string.IsNullOrEmpty(context.QnhLow))
                        {
                            context.RaiseError("QnhLow", "Value is required.");
                        }

                        if (string.IsNullOrEmpty(context.QnhHigh))
                        {
                            context.RaiseError("QnhHigh", "Value is required.");
                        }

                        if (string.IsNullOrEmpty(context.TransitionLevel))
                        {
                            context.RaiseError("TransitionLevel", "Value is required.");
                        }

                        int.TryParse(context.QnhLow, out var intLow);
                        int.TryParse(context.QnhHigh, out var intHigh);
                        int.TryParse(context.TransitionLevel, out var intLevel);

                        if (SelectedStation.AtisFormat.TransitionLevel.Values.Any(x =>
                                x.Low == intLow && x.High == intHigh))
                        {
                            context.RaiseError("QnhLow", "Duplicate transition level.");
                        }

                        if (context.HasErrors)
                            return;

                        SelectedStation.AtisFormat.TransitionLevel.Values.Add(
                            new TransitionLevelMeta(intLow, intHigh, intLevel));
                        
                        TransitionLevels = [];
                        var sorted = SelectedStation.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low);
                        foreach (var item in sorted)
                        {
                            TransitionLevels.Add(item);
                        }

                        if (mSessionManager.CurrentProfile != null)
                            mProfileRepository.Save(mSessionManager.CurrentProfile);
                    }
                };
                await dialog.ShowDialog((Window)DialogOwner);
            }
        }
    }

    private async Task HandleDeleteTransitionLevel(TransitionLevelMeta? obj)
    {
        if (obj == null || TransitionLevels == null || DialogOwner == null || SelectedStation == null)
            return;

        if (await MessageBox.ShowDialog((Window)DialogOwner,
                "Are you sure you want to delete the selected transition level?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (TransitionLevels.Remove(obj))
            {
                SelectedStation.AtisFormat.TransitionLevel.Values.Remove(obj);
                if (mSessionManager.CurrentProfile != null)
                    mProfileRepository.Save(mSessionManager.CurrentProfile);
            }
        }
    }

    private void HandleCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            HasUnsavedChanges = true;
        }
    }

    private void HandleTemplateVariableClicked(string? variable)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var topLevel = TopLevel.GetTopLevel(lifetime.MainWindow);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();
            
            if (focusedElement is TemplateVariableTextBox focusedTextBox)
            {
                focusedTextBox.Text += variable?.Replace("__", "_");
                focusedTextBox.CaretIndex = focusedTextBox.Text.Length;
            }
            
            if (focusedElement is AvaloniaEdit.Editing.TextArea focusedTextEditor)
            {
                focusedTextEditor.Document.Text += variable?.Replace("__", "_");
                focusedTextEditor.Caret.Offset = focusedTextEditor.Document.Text.Length;
            }
        }
    }

    public bool ApplyChanges()
    {
        if (SelectedStation == null)
            return true;

        ClearAllErrors();

        if (!string.IsNullOrEmpty(RoutineObservationTime))
        {
            var observationTimes = new List<int>();
            foreach (var interval in RoutineObservationTime.Split(','))
            {
                if (int.TryParse(interval, out var value))
                {
                    if (value < 0 || value > 59)
                    {
                        RaiseError(nameof(RoutineObservationTime), "Invalid routine observation time. Time values must between 0 and 59.");
                        return false;
                    }
                    if (observationTimes.Contains(value))
                    {
                        RaiseError(nameof(RoutineObservationTime), "Duplicate routine observation time values.");
                        return false;
                    }
                    observationTimes.Add(value);
                }
            }
            SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = observationTimes;
        }
        else
        {
            SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = null;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero != SpeakWindSpeedLeadingZero)
            SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero = SpeakWindSpeedLeadingZero;

        if (SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled != MagneticVariationEnabled)
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = MagneticVariationEnabled;

        if (int.TryParse(MagneticVariationValue, out var magneticVariation))
        {
            if (SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees != magneticVariation)
                SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = magneticVariation;
        }
        else
        {
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = false;
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = 0;
        }

        if (SelectedStation.AtisFormat.ObservationTime.Template.Text != ObservationTimeTextTemplate)
            SelectedStation.AtisFormat.ObservationTime.Template.Text = ObservationTimeTextTemplate;

        if (SelectedStation.AtisFormat.ObservationTime.Template.Voice != ObservationTimeVoiceTemplate)
            SelectedStation.AtisFormat.ObservationTime.Template.Voice = ObservationTimeVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text != StandardWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text = StandardWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice != StandardWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice = StandardWindVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text != StandardGustWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text = StandardGustWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice != StandardGustWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice = StandardGustWindVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text != VariableWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text = VariableWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice != VariableWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice = VariableWindVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text != VariableGustWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text = VariableGustWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice != VariableGustWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice = VariableGustWindVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text != VariableDirectionWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text = VariableDirectionWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice != VariableDirectionWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice = VariableDirectionWindVoiceTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text != CalmWindTextTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text = CalmWindTextTemplate;

        if (SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice != CalmWindVoiceTemplate)
            SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice = CalmWindVoiceTemplate;

        if (int.TryParse(CalmWindSpeed, out var speed))
        {
            if (SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed != speed)
            {
                SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed = speed;
            }
        }

        if (SelectedStation.AtisFormat.Visibility.Template.Text != VisibilityTextTemplate)
            SelectedStation.AtisFormat.Visibility.Template.Text = VisibilityTextTemplate;

        if (SelectedStation.AtisFormat.Visibility.Template.Voice != VisibilityVoiceTemplate)
            SelectedStation.AtisFormat.Visibility.Template.Voice = VisibilityVoiceTemplate;

        if (SelectedStation.AtisFormat.PresentWeather.Template.Text != PresentWeatherTextTemplate)
            SelectedStation.AtisFormat.PresentWeather.Template.Text = PresentWeatherTextTemplate;

        if (SelectedStation.AtisFormat.PresentWeather.Template.Voice != PresentWeatherVoiceTemplate)
            SelectedStation.AtisFormat.PresentWeather.Template.Voice = PresentWeatherVoiceTemplate;

        if (SelectedStation.AtisFormat.Clouds.Template.Text != CloudsTextTemplate)
            SelectedStation.AtisFormat.Clouds.Template.Text = CloudsTextTemplate;

        if (SelectedStation.AtisFormat.Clouds.Template.Voice != CloudsVoiceTemplate)
            SelectedStation.AtisFormat.Clouds.Template.Voice = CloudsVoiceTemplate;

        if (SelectedStation.AtisFormat.Temperature.Template.Text != TemperatureTextTemplate)
            SelectedStation.AtisFormat.Temperature.Template.Text = TemperatureTextTemplate;

        if (SelectedStation.AtisFormat.Temperature.Template.Voice != TemperatureVoiceTemplate)
            SelectedStation.AtisFormat.Temperature.Template.Voice = TemperatureVoiceTemplate;

        if (SelectedStation.AtisFormat.Dewpoint.Template.Text != DewpointTextTemplate)
            SelectedStation.AtisFormat.Dewpoint.Template.Text = DewpointTextTemplate;

        if (SelectedStation.AtisFormat.Dewpoint.Template.Voice != DewpointVoiceTemplate)
            SelectedStation.AtisFormat.Dewpoint.Template.Voice = DewpointVoiceTemplate;

        if (SelectedStation.AtisFormat.Altimeter.Template.Text != AltimeterTextTemplate)
            SelectedStation.AtisFormat.Altimeter.Template.Text = AltimeterTextTemplate;

        if (SelectedStation.AtisFormat.Altimeter.Template.Voice != AltimeterVoiceTemplate)
            SelectedStation.AtisFormat.Altimeter.Template.Voice = AltimeterVoiceTemplate;

        if (SelectedStation.AtisFormat.ClosingStatement.Template.Text != ClosingStatementTextTemplate)
            SelectedStation.AtisFormat.ClosingStatement.Template.Text = ClosingStatementTextTemplate;

        if (SelectedStation.AtisFormat.ClosingStatement.Template.Voice != ClosingStatementVoiceTemplate)
            SelectedStation.AtisFormat.ClosingStatement.Template.Voice = ClosingStatementVoiceTemplate;

        if (SelectedStation.AtisFormat.Visibility.North != VisibilityNorth)
            SelectedStation.AtisFormat.Visibility.North = VisibilityNorth ?? "";

        if (SelectedStation.AtisFormat.Visibility.NorthEast != VisibilityNorthEast)
            SelectedStation.AtisFormat.Visibility.NorthEast = VisibilityNorthEast ?? "";

        if (SelectedStation.AtisFormat.Visibility.East != VisibilityEast)
            SelectedStation.AtisFormat.Visibility.East = VisibilityEast ?? "";

        if (SelectedStation.AtisFormat.Visibility.SouthEast != VisibilitySouthEast)
            SelectedStation.AtisFormat.Visibility.SouthEast = VisibilitySouthEast ?? "";

        if (SelectedStation.AtisFormat.Visibility.South != VisibilitySouth)
            SelectedStation.AtisFormat.Visibility.South = VisibilitySouth ?? "";

        if (SelectedStation.AtisFormat.Visibility.SouthWest != VisibilitySouthWest)
            SelectedStation.AtisFormat.Visibility.SouthWest = VisibilitySouthWest ?? "";

        if (SelectedStation.AtisFormat.Visibility.West != VisibilityWest)
            SelectedStation.AtisFormat.Visibility.West = VisibilityWest ?? "";

        if (SelectedStation.AtisFormat.Visibility.NorthWest != VisibilityNorthWest)
            SelectedStation.AtisFormat.Visibility.NorthWest = VisibilityNorthWest ?? "";

        if (SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice != VisibilityUnlimitedVisibilityVoice)
            SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice = VisibilityUnlimitedVisibilityVoice ?? "";

        if (SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText != VisibilityUnlimitedVisibilityText)
            SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText = VisibilityUnlimitedVisibilityText ?? "";

        if (SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix != VisibilityIncludeVisibilitySuffix)
            SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix = VisibilityIncludeVisibilitySuffix;

        if (SelectedStation.AtisFormat.Visibility.MetersCutoff != VisibilityMetersCutoff)
            SelectedStation.AtisFormat.Visibility.MetersCutoff = VisibilityMetersCutoff;

        if (SelectedStation.AtisFormat.PresentWeather.LightIntensity != PresentWeatherLightIntensity)
            SelectedStation.AtisFormat.PresentWeather.LightIntensity = PresentWeatherLightIntensity ?? "";

        if (SelectedStation.AtisFormat.PresentWeather.ModerateIntensity != PresentWeatherModerateIntensity)
            SelectedStation.AtisFormat.PresentWeather.ModerateIntensity = PresentWeatherModerateIntensity ?? "";

        if (SelectedStation.AtisFormat.PresentWeather.HeavyIntensity != PresentWeatherHeavyIntensity)
            SelectedStation.AtisFormat.PresentWeather.HeavyIntensity = PresentWeatherHeavyIntensity ?? "";

        if (SelectedStation.AtisFormat.PresentWeather.Vicinity != PresentWeatherVicinity)
            SelectedStation.AtisFormat.PresentWeather.Vicinity = PresentWeatherVicinity ?? "";

        if (PresentWeatherTypes != null && SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes !=
            PresentWeatherTypes.ToDictionary(x => x.Key, x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken)))
        {
            SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes =
                PresentWeatherTypes.ToDictionary(x => x.Key, x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken));
        }

        if (SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer != CloudsIdentifyCeilingLayer)
            SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer = CloudsIdentifyCeilingLayer;

        if (SelectedStation.AtisFormat.Clouds.ConvertToMetric != CloudsConvertToMetric)
            SelectedStation.AtisFormat.Clouds.ConvertToMetric = CloudsConvertToMetric;

        if(SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds != CloudHeightAltitudeInHundreds)
            SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds = CloudHeightAltitudeInHundreds;
        
        if (SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text != UndeterminedLayerAltitudeText)
            SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text = UndeterminedLayerAltitudeText ?? "";

        if (SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice != UndeterminedLayerAltitudeVoice)
            SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice = UndeterminedLayerAltitudeVoice ?? "";

        if (CloudTypes != null && SelectedStation.AtisFormat.Clouds.Types !=
            CloudTypes.ToDictionary(x => x.Acronym, meta => new CloudType(meta.Text, meta.Spoken)))
        {
            SelectedStation.AtisFormat.Clouds.Types =
                CloudTypes.ToDictionary(x => x.Acronym, meta => new CloudType(meta.Text, meta.Spoken));
        }

        if (ConvectiveCloudTypes != null && SelectedStation.AtisFormat.Clouds.ConvectiveTypes !=
            ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value))
        {
            SelectedStation.AtisFormat.Clouds.ConvectiveTypes =
                ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value);
        }

        if (SelectedStation.AtisFormat.Temperature.UsePlusPrefix != TemperatureUsePlusPrefix)
            SelectedStation.AtisFormat.Temperature.UsePlusPrefix = TemperatureUsePlusPrefix;

        if (SelectedStation.AtisFormat.Temperature.SpeakLeadingZero != TemperatureSpeakLeadingZero)
            SelectedStation.AtisFormat.Temperature.SpeakLeadingZero = TemperatureSpeakLeadingZero;

        if (SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix != DewpointUsePlusPrefix)
            SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix = DewpointUsePlusPrefix;

        if (SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero != DewpointSpeakLeadingZero)
            SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero = DewpointSpeakLeadingZero;

        if (SelectedStation.AtisFormat.Altimeter.PronounceDecimal != AltimeterSpeakDecimal)
            SelectedStation.AtisFormat.Altimeter.PronounceDecimal = AltimeterSpeakDecimal;

        if (SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement != ClosingStatementAutoIncludeClosingStatement)
            SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement = ClosingStatementAutoIncludeClosingStatement;

        if (HasErrors)
            return false;
        
        if (mSessionManager.CurrentProfile != null)
            mProfileRepository.Save(mSessionManager.CurrentProfile);

        HasUnsavedChanges = false;
        
        return true;
    }
}