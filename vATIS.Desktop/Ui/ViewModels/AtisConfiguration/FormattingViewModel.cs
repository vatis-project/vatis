// <copyright file="FormattingViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;
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

/// <summary>
/// Represents the view model for formatting configuration within the ATIS system.
/// </summary>
public class FormattingViewModel : ReactiveViewModelBase
{
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private ObservableCollection<string>? _formattingOptions;
    private AtisStation? _selectedStation;
    private bool _hasUnsavedChanges;
    private string? _selectedFormattingOption;
    private string? _routineObservationTime;
    private string? _observationTimeTextTemplate;
    private string? _observationTimeVoiceTemplate;
    private bool _speakWindSpeedLeadingZero;
    private bool _magneticVariationEnabled;
    private string? _magneticVariationValue;
    private string? _standardWindTextTemplate;
    private string? _standardWindVoiceTemplate;
    private string? _standardGustWindTextTemplate;
    private string? _standardGustWindVoiceTemplate;
    private string? _variableWindTextTemplate;
    private string? _variableWindVoiceTemplate;
    private string? _variableGustWindTextTemplate;
    private string? _variableGustWindVoiceTemplate;
    private string? _variableDirectionWindTextTemplate;
    private string? _variableDirectionWindVoiceTemplate;
    private string? _calmWindTextTemplate;
    private string? _calmWindVoiceTemplate;
    private string? _calmWindSpeed;
    private string? _visibilityTextTemplate;
    private string? _visibilityVoiceTemplate;
    private string? _presentWeatherTextTemplate;
    private string? _presentWeatherVoiceTemplate;
    private string? _recentWeatherTextTemplate;
    private string? _recentWeatherVoiceTemplate;
    private string? _cloudsTextTemplate;
    private string? _cloudsVoiceTemplate;
    private string? _temperatureTextTemplate;
    private string? _temperatureVoiceTemplate;
    private string? _dewpointTextTemplate;
    private string? _dewpointVoiceTemplate;
    private string? _altimeterTextTemplate;
    private string? _altimeterVoiceTemplate;
    private string? _closingStatementTextTemplate;
    private string? _closingStatementVoiceTemplate;
    private string? _notamsTextTemplate;
    private string? _notamsVoiceTemplate;
    private string? _visibilityNorth;
    private string? _visibilityNorthEast;
    private string? _visibilityEast;
    private string? _visibilitySouthEast;
    private string? _visibilitySouth;
    private string? _visibilitySouthWest;
    private string? _visibilityWest;
    private string? _visibilityNorthWest;
    private string? _visibilityUnlimitedVisibilityVoice;
    private string? _visibilityUnlimitedVisibilityText;
    private bool _visibilityIncludeVisibilitySuffix;
    private int _visibilityMetersCutoff;
    private string? _presentWeatherLightIntensity;
    private string? _presentWeatherModerateIntensity;
    private string? _presentWeatherHeavyIntensity;
    private string? _presentWeatherVicinity;
    private bool _cloudsIdentifyCeilingLayer;
    private bool _cloudsConvertToMetric;
    private string? _undeterminedLayerAltitudeText;
    private string? _undeterminedLayerAltitudeVoice;
    private bool _cloudHeightAltitudeInHundreds;
    private bool _temperatureUsePlusPrefix;
    private bool _temperatureSpeakLeadingZero;
    private bool _dewpointUsePlusPrefix;
    private bool _dewpointSpeakLeadingZero;
    private bool _altimeterSpeakDecimal;
    private bool _closingStatementAutoIncludeClosingStatement;
    private ObservableCollection<TransitionLevelMeta>? _transitionLevelMetas;
    private string? _transitionLevelTextTemplate;
    private string? _transitionLevelVoiceTemplate;
    private ObservableCollection<PresentWeatherMeta>? _presentWeatherTypes;
    private ObservableCollection<CloudTypeMeta>? _cloudTypes;
    private ObservableCollection<ConvectiveCloudTypeMeta>? _convectiveCloudTypes;
    private List<ICompletionData> _contractionCompletionData = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">An instance of <see cref="IWindowFactory"/> for creating windows in the UI layer.</param>
    /// <param name="profileRepository">An instance of <see cref="IProfileRepository"/> for managing user profiles.</param>
    /// <param name="sessionManager">An instance of <see cref="ISessionManager"/> for managing session-related data and operations.</param>
    public FormattingViewModel(
        IWindowFactory windowFactory,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        _windowFactory = windowFactory;
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;

        FormattingOptions = [];

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        TemplateVariableClicked = ReactiveCommand.Create<string>(HandleTemplateVariableClicked);
        CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(HandleCellEditEnding);
        AddTransitionLevelCommand = ReactiveCommand.CreateFromTask(HandleAddTransitionLevel);
        DeleteTransitionLevelCommand =
            ReactiveCommand.CreateFromTask<TransitionLevelMeta>(HandleDeleteTransitionLevel);
    }

    /// <summary>
    /// Gets or sets the dialog owner used for displaying dialogs within the view model.
    /// </summary>
    public IDialogOwner? DialogOwner { get; set; }

    /// <summary>
    /// Gets the command executed when the ATIS station changes.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets the command executed when a template variable is clicked.
    /// </summary>
    public ReactiveCommand<string, Unit> TemplateVariableClicked { get; }

    /// <summary>
    /// Gets the command executed when cell editing ends in a data grid.
    /// </summary>
    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }

    /// <summary>
    /// Gets the command used to add a transition level in the view model.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddTransitionLevelCommand { get; }

    /// <summary>
    /// Gets the command used to delete a transition level from the configuration.
    /// </summary>
    public ReactiveCommand<TransitionLevelMeta, Unit> DeleteTransitionLevelCommand { get; }

    /// <summary>
    /// Gets or sets the collection of formatting options available in the view model.
    /// </summary>
    public ObservableCollection<string>? FormattingOptions
    {
        get => _formattingOptions;
        set => this.RaiseAndSetIfChanged(ref _formattingOptions, value);
    }

    /// <summary>
    /// Gets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        private set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the currently selected formatting option from the collection.
    /// </summary>
    public string? SelectedFormattingOption
    {
        get => _selectedFormattingOption;
        set => this.RaiseAndSetIfChanged(ref _selectedFormattingOption, value);
    }

    /// <summary>
    /// Gets or sets the routine observation time.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the observation time text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the observation time voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to speak wind speed leading zero.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether magnetic variation is enabled.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the magnetic variation value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the standard wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the standard wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the standard gust wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the standard gust wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable gust wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable gust wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable direction wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the variable direction wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the calm wind text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the calm wind voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the calm wind speed.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the recent weather text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the recent weather voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the clouds text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the clouds voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the temperature text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the temperature voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the dewpoint text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the dewpoint voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the altimeter text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the altimeter voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the closing statement text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the closing statement voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the NOTAMs text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the NOTAMs voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility north.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility north-east.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility east.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility south-east.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility south.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility south-west.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility west.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility north-west.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the unlimited visibility voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the unlimited visibility text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to include visibility suffix.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the visibility meters cutoff.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather light intensity.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather moderate intensity.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather heavy intensity.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather vicinity.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to identify ceiling layer.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to convert clouds to metric.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the undetermined layer altitude text.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the undetermined layer altitude voice.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether cloud height altitude is in hundreds.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to use plus prefix for temperature.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to speak leading zero for temperature.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to use plus prefix for dewpoint.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to speak leading zero for dewpoint.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether to speak decimal for altimeter.
    /// </summary>
    public bool AltimeterSpeakDecimal
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

    /// <summary>
    /// Gets or sets a value indicating whether to auto-include closing statement.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the transition levels.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the transition level text template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the transition level voice template.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the present weather types.
    /// </summary>
    public ObservableCollection<PresentWeatherMeta>? PresentWeatherTypes
    {
        get => _presentWeatherTypes;
        set => this.RaiseAndSetIfChanged(ref _presentWeatherTypes, value);
    }

    /// <summary>
    /// Gets or sets the cloud types.
    /// </summary>
    public ObservableCollection<CloudTypeMeta>? CloudTypes
    {
        get => _cloudTypes;
        set => this.RaiseAndSetIfChanged(ref _cloudTypes, value);
    }

    /// <summary>
    /// Gets or sets the convective cloud types.
    /// </summary>
    public ObservableCollection<ConvectiveCloudTypeMeta>? ConvectiveCloudTypes
    {
        get => _convectiveCloudTypes;
        set => this.RaiseAndSetIfChanged(ref _convectiveCloudTypes, value);
    }

    /// <summary>
    /// Gets or sets the contraction completion data.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Applies pending, unsaved changes.
    /// </summary>
    /// <returns>A value indicating whether changes are applied.</returns>
    public bool ApplyChanges()
    {
        if (SelectedStation == null)
        {
            return true;
        }

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
                        RaiseError(
                            nameof(RoutineObservationTime),
                            "Invalid routine observation time. Time values must between 0 and 59.");
                        return false;
                    }

                    if (observationTimes.Contains(value))
                    {
                        RaiseError(
                            nameof(RoutineObservationTime),
                            "Duplicate routine observation time values.");
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
        {
            SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero = SpeakWindSpeedLeadingZero;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled != MagneticVariationEnabled)
        {
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = MagneticVariationEnabled;
        }

        if (int.TryParse(MagneticVariationValue, out var magneticVariation))
        {
            if (SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees != magneticVariation)
            {
                SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = magneticVariation;
            }
        }
        else
        {
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = false;
            SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = 0;
        }

        if (SelectedStation.AtisFormat.ObservationTime.Template.Text != ObservationTimeTextTemplate)
        {
            SelectedStation.AtisFormat.ObservationTime.Template.Text = ObservationTimeTextTemplate;
        }

        if (SelectedStation.AtisFormat.ObservationTime.Template.Voice != ObservationTimeVoiceTemplate)
        {
            SelectedStation.AtisFormat.ObservationTime.Template.Voice = ObservationTimeVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text != StandardWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text = StandardWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice != StandardWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice = StandardWindVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text != StandardGustWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text = StandardGustWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice !=
            StandardGustWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice =
                StandardGustWindVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text != VariableWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text = VariableWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice != VariableWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice = VariableWindVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text != VariableGustWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text = VariableGustWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice !=
            VariableGustWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice =
                VariableGustWindVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text !=
            VariableDirectionWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text =
                VariableDirectionWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice !=
            VariableDirectionWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice =
                VariableDirectionWindVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text != CalmWindTextTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text = CalmWindTextTemplate;
        }

        if (SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice != CalmWindVoiceTemplate)
        {
            SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice = CalmWindVoiceTemplate;
        }

        if (int.TryParse(CalmWindSpeed, out var speed))
        {
            if (SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed != speed)
            {
                SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed = speed;
            }
        }

        if (SelectedStation.AtisFormat.Visibility.Template.Text != VisibilityTextTemplate)
        {
            SelectedStation.AtisFormat.Visibility.Template.Text = VisibilityTextTemplate;
        }

        if (SelectedStation.AtisFormat.Visibility.Template.Voice != VisibilityVoiceTemplate)
        {
            SelectedStation.AtisFormat.Visibility.Template.Voice = VisibilityVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.PresentWeather.Template.Text != PresentWeatherTextTemplate)
        {
            SelectedStation.AtisFormat.PresentWeather.Template.Text = PresentWeatherTextTemplate;
        }

        if (SelectedStation.AtisFormat.PresentWeather.Template.Voice != PresentWeatherVoiceTemplate)
        {
            SelectedStation.AtisFormat.PresentWeather.Template.Voice = PresentWeatherVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.RecentWeather.Template.Text != RecentWeatherTextTemplate)
        {
            SelectedStation.AtisFormat.RecentWeather.Template.Text = RecentWeatherTextTemplate;
        }

        if (SelectedStation.AtisFormat.RecentWeather.Template.Voice != RecentWeatherVoiceTemplate)
        {
            SelectedStation.AtisFormat.RecentWeather.Template.Voice = RecentWeatherVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Clouds.Template.Text != CloudsTextTemplate)
        {
            SelectedStation.AtisFormat.Clouds.Template.Text = CloudsTextTemplate;
        }

        if (SelectedStation.AtisFormat.Clouds.Template.Voice != CloudsVoiceTemplate)
        {
            SelectedStation.AtisFormat.Clouds.Template.Voice = CloudsVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Temperature.Template.Text != TemperatureTextTemplate)
        {
            SelectedStation.AtisFormat.Temperature.Template.Text = TemperatureTextTemplate;
        }

        if (SelectedStation.AtisFormat.Temperature.Template.Voice != TemperatureVoiceTemplate)
        {
            SelectedStation.AtisFormat.Temperature.Template.Voice = TemperatureVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Dewpoint.Template.Text != DewpointTextTemplate)
        {
            SelectedStation.AtisFormat.Dewpoint.Template.Text = DewpointTextTemplate;
        }

        if (SelectedStation.AtisFormat.Dewpoint.Template.Voice != DewpointVoiceTemplate)
        {
            SelectedStation.AtisFormat.Dewpoint.Template.Voice = DewpointVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Altimeter.Template.Text != AltimeterTextTemplate)
        {
            SelectedStation.AtisFormat.Altimeter.Template.Text = AltimeterTextTemplate;
        }

        if (SelectedStation.AtisFormat.Altimeter.Template.Voice != AltimeterVoiceTemplate)
        {
            SelectedStation.AtisFormat.Altimeter.Template.Voice = AltimeterVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.ClosingStatement.Template.Text != ClosingStatementTextTemplate)
        {
            SelectedStation.AtisFormat.ClosingStatement.Template.Text = ClosingStatementTextTemplate;
        }

        if (SelectedStation.AtisFormat.ClosingStatement.Template.Voice != ClosingStatementVoiceTemplate)
        {
            SelectedStation.AtisFormat.ClosingStatement.Template.Voice = ClosingStatementVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Visibility.North != VisibilityNorth)
        {
            SelectedStation.AtisFormat.Visibility.North = VisibilityNorth ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.NorthEast != VisibilityNorthEast)
        {
            SelectedStation.AtisFormat.Visibility.NorthEast = VisibilityNorthEast ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.East != VisibilityEast)
        {
            SelectedStation.AtisFormat.Visibility.East = VisibilityEast ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.SouthEast != VisibilitySouthEast)
        {
            SelectedStation.AtisFormat.Visibility.SouthEast = VisibilitySouthEast ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.South != VisibilitySouth)
        {
            SelectedStation.AtisFormat.Visibility.South = VisibilitySouth ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.SouthWest != VisibilitySouthWest)
        {
            SelectedStation.AtisFormat.Visibility.SouthWest = VisibilitySouthWest ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.West != VisibilityWest)
        {
            SelectedStation.AtisFormat.Visibility.West = VisibilityWest ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.NorthWest != VisibilityNorthWest)
        {
            SelectedStation.AtisFormat.Visibility.NorthWest = VisibilityNorthWest ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice !=
            VisibilityUnlimitedVisibilityVoice)
        {
            SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice =
                VisibilityUnlimitedVisibilityVoice ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText !=
            VisibilityUnlimitedVisibilityText)
        {
            SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText =
                VisibilityUnlimitedVisibilityText ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix !=
            VisibilityIncludeVisibilitySuffix)
        {
            SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix = VisibilityIncludeVisibilitySuffix;
        }

        if (SelectedStation.AtisFormat.Visibility.MetersCutoff != VisibilityMetersCutoff)
        {
            SelectedStation.AtisFormat.Visibility.MetersCutoff = VisibilityMetersCutoff;
        }

        if (SelectedStation.AtisFormat.PresentWeather.LightIntensity != PresentWeatherLightIntensity)
        {
            SelectedStation.AtisFormat.PresentWeather.LightIntensity =
                PresentWeatherLightIntensity ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.PresentWeather.ModerateIntensity != PresentWeatherModerateIntensity)
        {
            SelectedStation.AtisFormat.PresentWeather.ModerateIntensity =
                PresentWeatherModerateIntensity ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.PresentWeather.HeavyIntensity != PresentWeatherHeavyIntensity)
        {
            SelectedStation.AtisFormat.PresentWeather.HeavyIntensity =
                PresentWeatherHeavyIntensity ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.PresentWeather.Vicinity != PresentWeatherVicinity)
        {
            SelectedStation.AtisFormat.PresentWeather.Vicinity = PresentWeatherVicinity ?? string.Empty;
        }

        if (PresentWeatherTypes != null && SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes !=
            PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken)))
        {
            SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes = PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken));
        }

        if (SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer != CloudsIdentifyCeilingLayer)
        {
            SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer = CloudsIdentifyCeilingLayer;
        }

        if (SelectedStation.AtisFormat.Clouds.ConvertToMetric != CloudsConvertToMetric)
        {
            SelectedStation.AtisFormat.Clouds.ConvertToMetric = CloudsConvertToMetric;
        }

        if (SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds != CloudHeightAltitudeInHundreds)
        {
            SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds = CloudHeightAltitudeInHundreds;
        }

        if (SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text != UndeterminedLayerAltitudeText)
        {
            SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text =
                UndeterminedLayerAltitudeText ?? string.Empty;
        }

        if (SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice !=
            UndeterminedLayerAltitudeVoice)
        {
            SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice =
                UndeterminedLayerAltitudeVoice ?? string.Empty;
        }

        if (CloudTypes != null && SelectedStation.AtisFormat.Clouds.Types != CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken)))
        {
            SelectedStation.AtisFormat.Clouds.Types = CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken));
        }

        if (ConvectiveCloudTypes != null && SelectedStation.AtisFormat.Clouds.ConvectiveTypes !=
            ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value))
        {
            SelectedStation.AtisFormat.Clouds.ConvectiveTypes =
                ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value);
        }

        if (SelectedStation.AtisFormat.Temperature.UsePlusPrefix != TemperatureUsePlusPrefix)
        {
            SelectedStation.AtisFormat.Temperature.UsePlusPrefix = TemperatureUsePlusPrefix;
        }

        if (SelectedStation.AtisFormat.Temperature.SpeakLeadingZero != TemperatureSpeakLeadingZero)
        {
            SelectedStation.AtisFormat.Temperature.SpeakLeadingZero = TemperatureSpeakLeadingZero;
        }

        if (SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix != DewpointUsePlusPrefix)
        {
            SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix = DewpointUsePlusPrefix;
        }

        if (SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero != DewpointSpeakLeadingZero)
        {
            SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero = DewpointSpeakLeadingZero;
        }

        if (SelectedStation.AtisFormat.Altimeter.PronounceDecimal != AltimeterSpeakDecimal)
        {
            SelectedStation.AtisFormat.Altimeter.PronounceDecimal = AltimeterSpeakDecimal;
        }

        if (SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement !=
            ClosingStatementAutoIncludeClosingStatement)
        {
            SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement =
                ClosingStatementAutoIncludeClosingStatement;
        }

        if (SelectedStation.AtisFormat.TransitionLevel.Template.Text != TransitionLevelTextTemplate)
        {
            SelectedStation.AtisFormat.TransitionLevel.Template.Text = TransitionLevelTextTemplate;
        }

        if (SelectedStation.AtisFormat.TransitionLevel.Template.Voice != TransitionLevelVoiceTemplate)
        {
            SelectedStation.AtisFormat.TransitionLevel.Template.Voice = TransitionLevelVoiceTemplate;
        }

        if (SelectedStation.AtisFormat.Notams.Template.Text != NotamsTextTemplate)
        {
            SelectedStation.AtisFormat.Notams.Template.Text = NotamsTextTemplate;
        }

        if (SelectedStation.AtisFormat.Notams.Template.Voice != NotamsVoiceTemplate)
        {
            SelectedStation.AtisFormat.Notams.Template.Voice = NotamsVoiceTemplate;
        }

        if (HasErrors)
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

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

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
        ClosingStatementAutoIncludeClosingStatement =
            station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement;

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
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

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

                        if (SelectedStation.AtisFormat.TransitionLevel.Values.Any(
                                x =>
                                    x.Low == intLow && x.High == intHigh))
                        {
                            context.RaiseError("QnhLow", "Duplicate transition level.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        SelectedStation.AtisFormat.TransitionLevel.Values.Add(
                            new TransitionLevelMeta(intLow, intHigh, intLevel));

                        TransitionLevels = [];
                        var sorted = SelectedStation.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low);
                        foreach (var item in sorted)
                        {
                            TransitionLevels.Add(item);
                        }

                        if (_sessionManager.CurrentProfile != null)
                        {
                            _profileRepository.Save(_sessionManager.CurrentProfile);
                        }
                    }
                };
                await dialog.ShowDialog((Window)DialogOwner);
            }
        }
    }

    private async Task HandleDeleteTransitionLevel(TransitionLevelMeta? obj)
    {
        if (obj == null || TransitionLevels == null || DialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)DialogOwner,
                "Are you sure you want to delete the selected transition level?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (TransitionLevels.Remove(obj))
            {
                SelectedStation.AtisFormat.TransitionLevel.Values.Remove(obj);
                if (_sessionManager.CurrentProfile != null)
                {
                    _profileRepository.Save(_sessionManager.CurrentProfile);
                }
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
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(lifetime.MainWindow);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();

            if (focusedElement is TemplateVariableTextBox focusedTextBox)
            {
                focusedTextBox.Text += variable?.Replace("__", "_");
                focusedTextBox.CaretIndex = focusedTextBox.Text.Length;
            }

            if (focusedElement is TextArea focusedTextEditor)
            {
                focusedTextEditor.Document.Text += variable?.Replace("__", "_");
                focusedTextEditor.Caret.Offset = focusedTextEditor.Document.Text.Length;
            }
        }
    }
}
