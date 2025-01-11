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
    private readonly IWindowFactory _windowFactory;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly HashSet<string> _initializedProperties = [];

    public IDialogOwner? DialogOwner { get; set; }
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    public ReactiveCommand<string, Unit> TemplateVariableClicked { get;  }
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }
    public ReactiveCommand<Unit, Unit> AddTransitionLevelCommand { get;  }
    public ReactiveCommand<TransitionLevelMeta, Unit> DeleteTransitionLevelCommand { get;  }

    #region UI Properties
    private ObservableCollection<string>? _formattingOptions;
    public ObservableCollection<string>? FormattingOptions
    {
        get => _formattingOptions;
        set => this.RaiseAndSetIfChanged(ref _formattingOptions, value);
    }

    private AtisStation? _selectedStation;
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        private set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    private string? _selectedFormattingOption;
    public string? SelectedFormattingOption
    {
        get => _selectedFormattingOption;
        set => this.RaiseAndSetIfChanged(ref _selectedFormattingOption, value);
    }
    #endregion

    #region Config Properties
     private string? _routineObservationTime;
    public string? RoutineObservationTime
    {
        get => _routineObservationTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _routineObservationTime, value);
            if (!_initializedProperties.Add(nameof(RoutineObservationTime)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _observationTimeTextTemplate;
    public string? ObservationTimeTextTemplate
    {
        get => _observationTimeTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _observationTimeTextTemplate, value);
            if (!_initializedProperties.Add(nameof(ObservationTimeTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _observationTimeVoiceTemplate;
    public string? ObservationTimeVoiceTemplate
    {
        get => _observationTimeVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _observationTimeVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(ObservationTimeVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _speakWindSpeedLeadingZero;
    public bool SpeakWindSpeedLeadingZero
    {
        get => _speakWindSpeedLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref _speakWindSpeedLeadingZero, value);
            if (!_initializedProperties.Add(nameof(SpeakWindSpeedLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _magneticVariationEnabled;
    public bool MagneticVariationEnabled
    {
        get => _magneticVariationEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _magneticVariationEnabled, value);
            if (!_initializedProperties.Add(nameof(MagneticVariationEnabled)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _magneticVariationValue;
    public string? MagneticVariationValue
    {
        get => _magneticVariationValue;
        set
        {
            this.RaiseAndSetIfChanged(ref _magneticVariationValue, value);
            if (!_initializedProperties.Add(nameof(MagneticVariationValue)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardWindTextTemplate;
    public string? StandardWindTextTemplate
    {
        get => _standardWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _standardWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(StandardWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardWindVoiceTemplate;
    public string? StandardWindVoiceTemplate
    {
        get => _standardWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _standardWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(StandardWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardGustWindTextTemplate;
    public string? StandardGustWindTextTemplate
    {
        get => _standardGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _standardGustWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(StandardGustWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardGustWindVoiceTemplate;
    public string? StandardGustWindVoiceTemplate
    {
        get => _standardGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _standardGustWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(StandardGustWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableWindTextTemplate;
    public string? VariableWindTextTemplate
    {
        get => _variableWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableWindVoiceTemplate;
    public string? VariableWindVoiceTemplate
    {
        get => _variableWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableGustWindTextTemplate;
    public string? VariableGustWindTextTemplate
    {
        get => _variableGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableGustWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableGustWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableGustWindVoiceTemplate;
    public string? VariableGustWindVoiceTemplate
    {
        get => _variableGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableGustWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableGustWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableDirectionWindTextTemplate;
    public string? VariableDirectionWindTextTemplate
    {
        get => _variableDirectionWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableDirectionWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableDirectionWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableDirectionWindVoiceTemplate;
    public string? VariableDirectionWindVoiceTemplate
    {
        get => _variableDirectionWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _variableDirectionWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(VariableDirectionWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindTextTemplate;
    public string? CalmWindTextTemplate
    {
        get => _calmWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _calmWindTextTemplate, value);
            if (!_initializedProperties.Add(nameof(CalmWindTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindVoiceTemplate;
    public string? CalmWindVoiceTemplate
    {
        get => _calmWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _calmWindVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(CalmWindVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindSpeed;
    public string? CalmWindSpeed
    {
        get => _calmWindSpeed;
        set
        {
            this.RaiseAndSetIfChanged(ref _calmWindSpeed, value);
            if (!_initializedProperties.Add(nameof(CalmWindSpeed)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityTextTemplate;
    public string? VisibilityTextTemplate
    {
        get => _visibilityTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityTextTemplate, value);
            if (!_initializedProperties.Add(nameof(VisibilityTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityVoiceTemplate;
    public string? VisibilityVoiceTemplate
    {
        get => _visibilityVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(VisibilityVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherTextTemplate;
    public string? PresentWeatherTextTemplate
    {
        get => _presentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherTextTemplate, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherVoiceTemplate;
    public string? PresentWeatherVoiceTemplate
    {
        get => _presentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _recentWeatherTextTemplate;
    public string? RecentWeatherTextTemplate
    {
        get => _recentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _recentWeatherTextTemplate, value);
            if (!_initializedProperties.Add(nameof(RecentWeatherTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _recentWeatherVoiceTemplate;
    public string? RecentWeatherVoiceTemplate
    {
        get => _recentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _recentWeatherVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(RecentWeatherVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _cloudsTextTemplate;
    public string? CloudsTextTemplate
    {
        get => _cloudsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _cloudsTextTemplate, value);
            if (!_initializedProperties.Add(nameof(CloudsTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _cloudsVoiceTemplate;
    public string? CloudsVoiceTemplate
    {
        get => _cloudsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _cloudsVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(CloudsVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _temperatureTextTemplate;
    public string? TemperatureTextTemplate
    {
        get => _temperatureTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _temperatureTextTemplate, value);
            if (!_initializedProperties.Add(nameof(TemperatureTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _temperatureVoiceTemplate;
    public string? TemperatureVoiceTemplate
    {
        get => _temperatureVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _temperatureVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(TemperatureVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _dewpointTextTemplate;
    public string? DewpointTextTemplate
    {
        get => _dewpointTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _dewpointTextTemplate, value);
            if (!_initializedProperties.Add(nameof(DewpointTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _dewpointVoiceTemplate;
    public string? DewpointVoiceTemplate
    {
        get => _dewpointVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _dewpointVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(DewpointVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _altimeterTextTemplate;
    public string? AltimeterTextTemplate
    {
        get => _altimeterTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _altimeterTextTemplate, value);
            if (!_initializedProperties.Add(nameof(AltimeterTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _altimeterVoiceTemplate;
    public string? AltimeterVoiceTemplate
    {
        get => _altimeterVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _altimeterVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(AltimeterVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _closingStatementTextTemplate;
    public string? ClosingStatementTextTemplate
    {
        get => _closingStatementTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _closingStatementTextTemplate, value);
            if (!_initializedProperties.Add(nameof(ClosingStatementTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _closingStatementVoiceTemplate;
    public string? ClosingStatementVoiceTemplate
    {
        get => _closingStatementVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _closingStatementVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(ClosingStatementVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _notamsTextTemplate;
    public string? NotamsTextTemplate
    {
        get => _notamsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _notamsTextTemplate, value);
            if (!_initializedProperties.Add(nameof(NotamsTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _notamsVoiceTemplate;
    public string? NotamsVoiceTemplate
    {
        get => _notamsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _notamsVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(NotamsVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorth;
    public string? VisibilityNorth
    {
        get => _visibilityNorth;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityNorth, value);
            if (!_initializedProperties.Add(nameof(VisibilityNorth)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorthEast;
    public string? VisibilityNorthEast
    {
        get => _visibilityNorthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityNorthEast, value);
            if (!_initializedProperties.Add(nameof(VisibilityNorthEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityEast;
    public string? VisibilityEast
    {
        get => _visibilityEast;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityEast, value);
            if (!_initializedProperties.Add(nameof(VisibilityEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouthEast;
    public string? VisibilitySouthEast
    {
        get => _visibilitySouthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilitySouthEast, value);
            if (!_initializedProperties.Add(nameof(VisibilitySouthEast)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouth;
    public string? VisibilitySouth
    {
        get => _visibilitySouth;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilitySouth, value);
            if (!_initializedProperties.Add(nameof(VisibilitySouth)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouthWest;
    public string? VisibilitySouthWest
    {
        get => _visibilitySouthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilitySouthWest, value);
            if (!_initializedProperties.Add(nameof(VisibilitySouthWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityWest;
    public string? VisibilityWest
    {
        get => _visibilityWest;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityWest, value);
            if (!_initializedProperties.Add(nameof(VisibilityWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorthWest;
    public string? VisibilityNorthWest
    {
        get => _visibilityNorthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityNorthWest, value);
            if (!_initializedProperties.Add(nameof(VisibilityNorthWest)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityUnlimitedVisibilityVoice;
    public string? VisibilityUnlimitedVisibilityVoice
    {
        get => _visibilityUnlimitedVisibilityVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityUnlimitedVisibilityVoice, value);
            if (!_initializedProperties.Add(nameof(VisibilityUnlimitedVisibilityVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityUnlimitedVisibilityText;
    public string? VisibilityUnlimitedVisibilityText
    {
        get => _visibilityUnlimitedVisibilityText;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityUnlimitedVisibilityText, value);
            if (!_initializedProperties.Add(nameof(VisibilityUnlimitedVisibilityText)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _visibilityIncludeVisibilitySuffix;
    public bool VisibilityIncludeVisibilitySuffix
    {
        get => _visibilityIncludeVisibilitySuffix;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityIncludeVisibilitySuffix, value);
            if (!_initializedProperties.Add(nameof(VisibilityIncludeVisibilitySuffix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private int _visibilityMetersCutoff;
    public int VisibilityMetersCutoff
    {
        get => _visibilityMetersCutoff;
        set
        {
            this.RaiseAndSetIfChanged(ref _visibilityMetersCutoff, value);
            if (!_initializedProperties.Add(nameof(VisibilityMetersCutoff)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherLightIntensity;
    public string? PresentWeatherLightIntensity
    {
        get => _presentWeatherLightIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherLightIntensity, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherLightIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherModerateIntensity;
    public string? PresentWeatherModerateIntensity
    {
        get => _presentWeatherModerateIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherModerateIntensity, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherModerateIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherHeavyIntensity;
    public string? PresentWeatherHeavyIntensity
    {
        get => _presentWeatherHeavyIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherHeavyIntensity, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherHeavyIntensity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherVicinity;
    public string? PresentWeatherVicinity
    {
        get => _presentWeatherVicinity;
        set
        {
            this.RaiseAndSetIfChanged(ref _presentWeatherVicinity, value);
            if (!_initializedProperties.Add(nameof(PresentWeatherVicinity)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudsIdentifyCeilingLayer;
    public bool CloudsIdentifyCeilingLayer
    {
        get => _cloudsIdentifyCeilingLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref _cloudsIdentifyCeilingLayer, value);
            if (!_initializedProperties.Add(nameof(CloudsIdentifyCeilingLayer)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudsConvertToMetric;
    public bool CloudsConvertToMetric
    {
        get => _cloudsConvertToMetric;
        set
        {
            this.RaiseAndSetIfChanged(ref _cloudsConvertToMetric, value);
            if (!_initializedProperties.Add(nameof(CloudsConvertToMetric)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _undeterminedLayerAltitudeText;
    public string? UndeterminedLayerAltitudeText
    {
        get => _undeterminedLayerAltitudeText;
        set
        {
            this.RaiseAndSetIfChanged(ref _undeterminedLayerAltitudeText, value);
            if (!_initializedProperties.Add(nameof(UndeterminedLayerAltitudeText)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _undeterminedLayerAltitudeVoice;
    public string? UndeterminedLayerAltitudeVoice
    {
        get => _undeterminedLayerAltitudeVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref _undeterminedLayerAltitudeVoice, value);
            if (!_initializedProperties.Add(nameof(UndeterminedLayerAltitudeVoice)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudHeightAltitudeInHundreds;
    public bool CloudHeightAltitudeInHundreds
    {
        get => _cloudHeightAltitudeInHundreds;
        set
        {
            this.RaiseAndSetIfChanged(ref _cloudHeightAltitudeInHundreds, value);
            if (!_initializedProperties.Add(nameof(CloudHeightAltitudeInHundreds)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _temperatureUsePlusPrefix;
    public bool TemperatureUsePlusPrefix
    {
        get => _temperatureUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref _temperatureUsePlusPrefix, value);
            if (!_initializedProperties.Add(nameof(TemperatureUsePlusPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _temperatureSpeakLeadingZero;
    public bool TemperatureSpeakLeadingZero
    {
        get => _temperatureSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref _temperatureSpeakLeadingZero, value);
            if (!_initializedProperties.Add(nameof(TemperatureSpeakLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _dewpointUsePlusPrefix;
    public bool DewpointUsePlusPrefix
    {
        get => _dewpointUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref _dewpointUsePlusPrefix, value);
            if (!_initializedProperties.Add(nameof(DewpointUsePlusPrefix)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _dewpointSpeakLeadingZero;
    public bool DewpointSpeakLeadingZero
    {
        get => _dewpointSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref _dewpointSpeakLeadingZero, value);
            if (!_initializedProperties.Add(nameof(DewpointSpeakLeadingZero)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _altimeterSpeakDecimal;
    private bool AltimeterSpeakDecimal
    {
        get => _altimeterSpeakDecimal;
        set
        {
            this.RaiseAndSetIfChanged(ref _altimeterSpeakDecimal, value);
            if (!_initializedProperties.Add(nameof(AltimeterSpeakDecimal)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private bool _closingStatementAutoIncludeClosingStatement;
    public bool ClosingStatementAutoIncludeClosingStatement
    {
        get => _closingStatementAutoIncludeClosingStatement;
        set
        {
            this.RaiseAndSetIfChanged(ref _closingStatementAutoIncludeClosingStatement, value);
            if (!_initializedProperties.Add(nameof(ClosingStatementAutoIncludeClosingStatement)))
            {
                HasUnsavedChanges = true;
            }
        }
    }


    private ObservableCollection<TransitionLevelMeta>? _transitionLevelMetas;
    public ObservableCollection<TransitionLevelMeta>? TransitionLevels
    {
        get => _transitionLevelMetas;
        set
        {
            this.RaiseAndSetIfChanged(ref _transitionLevelMetas, value);
            if (!_initializedProperties.Add(nameof(TransitionLevels)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _transitionLevelTextTemplate;
    public string? TransitionLevelTextTemplate
    {
        get => _transitionLevelTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _transitionLevelTextTemplate, value);
            if (!_initializedProperties.Add(nameof(TransitionLevelTextTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private string? _transitionLevelVoiceTemplate;
    public string? TransitionLevelVoiceTemplate
    {
        get => _transitionLevelVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref _transitionLevelVoiceTemplate, value);
            if (!_initializedProperties.Add(nameof(TransitionLevelVoiceTemplate)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private ObservableCollection<PresentWeatherMeta>? _presentWeatherTypes;
    public ObservableCollection<PresentWeatherMeta>? PresentWeatherTypes
    {
        get => _presentWeatherTypes;
        set => this.RaiseAndSetIfChanged(ref _presentWeatherTypes, value);
    }

    private ObservableCollection<CloudTypeMeta>? _cloudTypes;
    public ObservableCollection<CloudTypeMeta>? CloudTypes
    {
        get => _cloudTypes;
        set => this.RaiseAndSetIfChanged(ref _cloudTypes, value);
    }

    private ObservableCollection<ConvectiveCloudTypeMeta>? _convectiveCloudTypes;
    public ObservableCollection<ConvectiveCloudTypeMeta>? ConvectiveCloudTypes
    {
        get => _convectiveCloudTypes;
        set => this.RaiseAndSetIfChanged(ref _convectiveCloudTypes, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];
    private List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }
    #endregion

    public FormattingViewModel(IWindowFactory windowFactory, IProfileRepository profileRepository, ISessionManager sessionManager)
    {
        _windowFactory = windowFactory;
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;

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
                "NOTAMs",
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
                "NOTAMs",
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
        RecentWeatherVoiceTemplate = station.AtisFormat.RecentWeather.Template.Voice;
        RecentWeatherTextTemplate = station.AtisFormat.RecentWeather.Template.Text;
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
        NotamsTextTemplate = station.AtisFormat.Notams.Template.Text;
        NotamsVoiceTemplate = station.AtisFormat.Notams.Template.Voice;
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

        TransitionLevelTextTemplate = station.AtisFormat.TransitionLevel.Template.Text;
        TransitionLevelVoiceTemplate = station.AtisFormat.TransitionLevel.Template.Voice;

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

            var dialog = _windowFactory.CreateTransitionLevelDialog();
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

                        if (_sessionManager.CurrentProfile != null)
                            _profileRepository.Save(_sessionManager.CurrentProfile);
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
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
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

        if(SelectedStation.AtisFormat.RecentWeather.Template.Text != RecentWeatherTextTemplate)
            SelectedStation.AtisFormat.RecentWeather.Template.Text = RecentWeatherTextTemplate;

        if(SelectedStation.AtisFormat.RecentWeather.Template.Voice != RecentWeatherVoiceTemplate)
            SelectedStation.AtisFormat.RecentWeather.Template.Voice = RecentWeatherVoiceTemplate;

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

        if (SelectedStation.AtisFormat.TransitionLevel.Template.Text != TransitionLevelTextTemplate)
            SelectedStation.AtisFormat.TransitionLevel.Template.Text = TransitionLevelTextTemplate;

        if (SelectedStation.AtisFormat.TransitionLevel.Template.Voice != TransitionLevelVoiceTemplate)
            SelectedStation.AtisFormat.TransitionLevel.Template.Voice = TransitionLevelVoiceTemplate;

        if (SelectedStation.AtisFormat.Notams.Template.Text != NotamsTextTemplate)
            SelectedStation.AtisFormat.Notams.Template.Text = NotamsTextTemplate;

        if (SelectedStation.AtisFormat.Notams.Template.Voice != NotamsVoiceTemplate)
            SelectedStation.AtisFormat.Notams.Template.Voice = NotamsVoiceTemplate;

        if (HasErrors)
            return false;

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedChanges = false;

        return true;
    }
}
