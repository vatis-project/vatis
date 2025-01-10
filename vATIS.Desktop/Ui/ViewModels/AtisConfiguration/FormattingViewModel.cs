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
    private readonly HashSet<string> initializedProperties = [];
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private readonly IWindowFactory windowFactory;
    private ObservableCollection<string>? formattingOptions;
    private AtisStation? selectedStation;
    private bool hasUnsavedChanges;
    private string? selectedFormattingOption;
    private string? routineObservationTime;
    private string? observationTimeTextTemplate;
    private string? observationTimeVoiceTemplate;
    private bool speakWindSpeedLeadingZero;
    private bool magneticVariationEnabled;
    private string? magneticVariationValue;
    private string? standardWindTextTemplate;
    private string? standardWindVoiceTemplate;
    private string? standardGustWindTextTemplate;
    private string? standardGustWindVoiceTemplate;
    private string? variableWindTextTemplate;
    private string? variableWindVoiceTemplate;
    private string? variableGustWindTextTemplate;
    private string? variableGustWindVoiceTemplate;
    private string? variableDirectionWindTextTemplate;
    private string? variableDirectionWindVoiceTemplate;
    private string? calmWindTextTemplate;
    private string? calmWindVoiceTemplate;
    private string? calmWindSpeed;
    private string? visibilityTextTemplate;
    private string? visibilityVoiceTemplate;
    private string? presentWeatherTextTemplate;
    private string? presentWeatherVoiceTemplate;
    private string? recentWeatherTextTemplate;
    private string? recentWeatherVoiceTemplate;
    private string? cloudsTextTemplate;
    private string? cloudsVoiceTemplate;
    private string? temperatureTextTemplate;
    private string? temperatureVoiceTemplate;
    private string? dewpointTextTemplate;
    private string? dewpointVoiceTemplate;
    private string? altimeterTextTemplate;
    private string? altimeterVoiceTemplate;
    private string? closingStatementTextTemplate;
    private string? closingStatementVoiceTemplate;
    private string? notamsTextTemplate;
    private string? notamsVoiceTemplate;
    private string? visibilityNorth;
    private string? visibilityNorthEast;
    private string? visibilityEast;
    private string? visibilitySouthEast;
    private string? visibilitySouth;
    private string? visibilitySouthWest;
    private string? visibilityWest;
    private string? visibilityNorthWest;
    private string? visibilityUnlimitedVisibilityVoice;
    private string? visibilityUnlimitedVisibilityText;
    private bool visibilityIncludeVisibilitySuffix;
    private int visibilityMetersCutoff;
    private string? presentWeatherLightIntensity;
    private string? presentWeatherModerateIntensity;
    private string? presentWeatherHeavyIntensity;
    private string? presentWeatherVicinity;
    private bool cloudsIdentifyCeilingLayer;
    private bool cloudsConvertToMetric;
    private string? undeterminedLayerAltitudeText;
    private string? undeterminedLayerAltitudeVoice;
    private bool cloudHeightAltitudeInHundreds;
    private bool temperatureUsePlusPrefix;
    private bool temperatureSpeakLeadingZero;
    private bool dewpointUsePlusPrefix;
    private bool dewpointSpeakLeadingZero;
    private bool altimeterSpeakDecimal;
    private bool closingStatementAutoIncludeClosingStatement;
    private ObservableCollection<TransitionLevelMeta>? transitionLevelMetas;
    private string? transitionLevelTextTemplate;
    private string? transitionLevelVoiceTemplate;
    private ObservableCollection<PresentWeatherMeta>? presentWeatherTypes;
    private ObservableCollection<CloudTypeMeta>? cloudTypes;
    private ObservableCollection<ConvectiveCloudTypeMeta>? convectiveCloudTypes;
    private List<ICompletionData> contractionCompletionData = new();

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
        this.windowFactory = windowFactory;
        this.profileRepository = profileRepository;
        this.sessionManager = sessionManager;

        this.FormattingOptions = [];

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleAtisStationChanged);
        this.TemplateVariableClicked = ReactiveCommand.Create<string>(this.HandleTemplateVariableClicked);
        this.CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(this.HandleCellEditEnding);
        this.AddTransitionLevelCommand = ReactiveCommand.CreateFromTask(this.HandleAddTransitionLevel);
        this.DeleteTransitionLevelCommand =
            ReactiveCommand.CreateFromTask<TransitionLevelMeta>(this.HandleDeleteTransitionLevel);
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
        get => this.formattingOptions;
        set => this.RaiseAndSetIfChanged(ref this.formattingOptions, value);
    }

    /// <summary>
    /// Gets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => this.selectedStation;
        private set => this.RaiseAndSetIfChanged(ref this.selectedStation, value);
    }

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => this.hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this.hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the currently selected formatting option from the collection.
    /// </summary>
    public string? SelectedFormattingOption
    {
        get => this.selectedFormattingOption;
        set => this.RaiseAndSetIfChanged(ref this.selectedFormattingOption, value);
    }

    /// <summary>
    /// Gets or sets the routine observation time.
    /// </summary>
    public string? RoutineObservationTime
    {
        get => this.routineObservationTime;
        set
        {
            this.RaiseAndSetIfChanged(ref this.routineObservationTime, value);
            if (!this.initializedProperties.Add(nameof(this.RoutineObservationTime)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the observation time text template.
    /// </summary>
    public string? ObservationTimeTextTemplate
    {
        get => this.observationTimeTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.observationTimeTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.ObservationTimeTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the observation time voice template.
    /// </summary>
    public string? ObservationTimeVoiceTemplate
    {
        get => this.observationTimeVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.observationTimeVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.ObservationTimeVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to speak wind speed leading zero.
    /// </summary>
    public bool SpeakWindSpeedLeadingZero
    {
        get => this.speakWindSpeedLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this.speakWindSpeedLeadingZero, value);
            if (!this.initializedProperties.Add(nameof(this.SpeakWindSpeedLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether magnetic variation is enabled.
    /// </summary>
    public bool MagneticVariationEnabled
    {
        get => this.magneticVariationEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref this.magneticVariationEnabled, value);
            if (!this.initializedProperties.Add(nameof(this.MagneticVariationEnabled)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the magnetic variation value.
    /// </summary>
    public string? MagneticVariationValue
    {
        get => this.magneticVariationValue;
        set
        {
            this.RaiseAndSetIfChanged(ref this.magneticVariationValue, value);
            if (!this.initializedProperties.Add(nameof(this.MagneticVariationValue)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the standard wind text template.
    /// </summary>
    public string? StandardWindTextTemplate
    {
        get => this.standardWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.standardWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.StandardWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the standard wind voice template.
    /// </summary>
    public string? StandardWindVoiceTemplate
    {
        get => this.standardWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.standardWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.StandardWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the standard gust wind text template.
    /// </summary>
    public string? StandardGustWindTextTemplate
    {
        get => this.standardGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.standardGustWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.StandardGustWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the standard gust wind voice template.
    /// </summary>
    public string? StandardGustWindVoiceTemplate
    {
        get => this.standardGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.standardGustWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.StandardGustWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable wind text template.
    /// </summary>
    public string? VariableWindTextTemplate
    {
        get => this.variableWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable wind voice template.
    /// </summary>
    public string? VariableWindVoiceTemplate
    {
        get => this.variableWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable gust wind text template.
    /// </summary>
    public string? VariableGustWindTextTemplate
    {
        get => this.variableGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableGustWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableGustWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable gust wind voice template.
    /// </summary>
    public string? VariableGustWindVoiceTemplate
    {
        get => this.variableGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableGustWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableGustWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable direction wind text template.
    /// </summary>
    public string? VariableDirectionWindTextTemplate
    {
        get => this.variableDirectionWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableDirectionWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableDirectionWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the variable direction wind voice template.
    /// </summary>
    public string? VariableDirectionWindVoiceTemplate
    {
        get => this.variableDirectionWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.variableDirectionWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VariableDirectionWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the calm wind text template.
    /// </summary>
    public string? CalmWindTextTemplate
    {
        get => this.calmWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.calmWindTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.CalmWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the calm wind voice template.
    /// </summary>
    public string? CalmWindVoiceTemplate
    {
        get => this.calmWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.calmWindVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.CalmWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the calm wind speed.
    /// </summary>
    public string? CalmWindSpeed
    {
        get => this.calmWindSpeed;
        set
        {
            this.RaiseAndSetIfChanged(ref this.calmWindSpeed, value);
            if (!this.initializedProperties.Add(nameof(this.CalmWindSpeed)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility text template.
    /// </summary>
    public string? VisibilityTextTemplate
    {
        get => this.visibilityTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility voice template.
    /// </summary>
    public string? VisibilityVoiceTemplate
    {
        get => this.visibilityVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather text template.
    /// </summary>
    public string? PresentWeatherTextTemplate
    {
        get => this.presentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather voice template.
    /// </summary>
    public string? PresentWeatherVoiceTemplate
    {
        get => this.presentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the recent weather text template.
    /// </summary>
    public string? RecentWeatherTextTemplate
    {
        get => this.recentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.recentWeatherTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.RecentWeatherTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the recent weather voice template.
    /// </summary>
    public string? RecentWeatherVoiceTemplate
    {
        get => this.recentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.recentWeatherVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.RecentWeatherVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the clouds text template.
    /// </summary>
    public string? CloudsTextTemplate
    {
        get => this.cloudsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.cloudsTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.CloudsTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the clouds voice template.
    /// </summary>
    public string? CloudsVoiceTemplate
    {
        get => this.cloudsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.cloudsVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.CloudsVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the temperature text template.
    /// </summary>
    public string? TemperatureTextTemplate
    {
        get => this.temperatureTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.temperatureTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.TemperatureTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the temperature voice template.
    /// </summary>
    public string? TemperatureVoiceTemplate
    {
        get => this.temperatureVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.temperatureVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.TemperatureVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the dewpoint text template.
    /// </summary>
    public string? DewpointTextTemplate
    {
        get => this.dewpointTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.dewpointTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.DewpointTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the dewpoint voice template.
    /// </summary>
    public string? DewpointVoiceTemplate
    {
        get => this.dewpointVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.dewpointVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.DewpointVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the altimeter text template.
    /// </summary>
    public string? AltimeterTextTemplate
    {
        get => this.altimeterTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.altimeterTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.AltimeterTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the altimeter voice template.
    /// </summary>
    public string? AltimeterVoiceTemplate
    {
        get => this.altimeterVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.altimeterVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.AltimeterVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the closing statement text template.
    /// </summary>
    public string? ClosingStatementTextTemplate
    {
        get => this.closingStatementTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.closingStatementTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.ClosingStatementTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the closing statement voice template.
    /// </summary>
    public string? ClosingStatementVoiceTemplate
    {
        get => this.closingStatementVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.closingStatementVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.ClosingStatementVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the NOTAMs text template.
    /// </summary>
    public string? NotamsTextTemplate
    {
        get => this.notamsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.notamsTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.NotamsTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the NOTAMs voice template.
    /// </summary>
    public string? NotamsVoiceTemplate
    {
        get => this.notamsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.notamsVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.NotamsVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility north.
    /// </summary>
    public string? VisibilityNorth
    {
        get => this.visibilityNorth;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityNorth, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityNorth)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility north-east.
    /// </summary>
    public string? VisibilityNorthEast
    {
        get => this.visibilityNorthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityNorthEast, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityNorthEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility east.
    /// </summary>
    public string? VisibilityEast
    {
        get => this.visibilityEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityEast, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility south-east.
    /// </summary>
    public string? VisibilitySouthEast
    {
        get => this.visibilitySouthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilitySouthEast, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilitySouthEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility south.
    /// </summary>
    public string? VisibilitySouth
    {
        get => this.visibilitySouth;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilitySouth, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilitySouth)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility south-west.
    /// </summary>
    public string? VisibilitySouthWest
    {
        get => this.visibilitySouthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilitySouthWest, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilitySouthWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility west.
    /// </summary>
    public string? VisibilityWest
    {
        get => this.visibilityWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityWest, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility north-west.
    /// </summary>
    public string? VisibilityNorthWest
    {
        get => this.visibilityNorthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityNorthWest, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityNorthWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the unlimited visibility voice template.
    /// </summary>
    public string? VisibilityUnlimitedVisibilityVoice
    {
        get => this.visibilityUnlimitedVisibilityVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityUnlimitedVisibilityVoice, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityUnlimitedVisibilityVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the unlimited visibility text template.
    /// </summary>
    public string? VisibilityUnlimitedVisibilityText
    {
        get => this.visibilityUnlimitedVisibilityText;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityUnlimitedVisibilityText, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityUnlimitedVisibilityText)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include visibility suffix.
    /// </summary>
    public bool VisibilityIncludeVisibilitySuffix
    {
        get => this.visibilityIncludeVisibilitySuffix;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityIncludeVisibilitySuffix, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityIncludeVisibilitySuffix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visibility meters cutoff.
    /// </summary>
    public int VisibilityMetersCutoff
    {
        get => this.visibilityMetersCutoff;
        set
        {
            this.RaiseAndSetIfChanged(ref this.visibilityMetersCutoff, value);
            if (!this.initializedProperties.Add(nameof(this.VisibilityMetersCutoff)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather light intensity.
    /// </summary>
    public string? PresentWeatherLightIntensity
    {
        get => this.presentWeatherLightIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherLightIntensity, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherLightIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather moderate intensity.
    /// </summary>
    public string? PresentWeatherModerateIntensity
    {
        get => this.presentWeatherModerateIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherModerateIntensity, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherModerateIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather heavy intensity.
    /// </summary>
    public string? PresentWeatherHeavyIntensity
    {
        get => this.presentWeatherHeavyIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherHeavyIntensity, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherHeavyIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather vicinity.
    /// </summary>
    public string? PresentWeatherVicinity
    {
        get => this.presentWeatherVicinity;
        set
        {
            this.RaiseAndSetIfChanged(ref this.presentWeatherVicinity, value);
            if (!this.initializedProperties.Add(nameof(this.PresentWeatherVicinity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to identify ceiling layer.
    /// </summary>
    public bool CloudsIdentifyCeilingLayer
    {
        get => this.cloudsIdentifyCeilingLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref this.cloudsIdentifyCeilingLayer, value);
            if (!this.initializedProperties.Add(nameof(this.CloudsIdentifyCeilingLayer)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to convert clouds to metric.
    /// </summary>
    public bool CloudsConvertToMetric
    {
        get => this.cloudsConvertToMetric;
        set
        {
            this.RaiseAndSetIfChanged(ref this.cloudsConvertToMetric, value);
            if (!this.initializedProperties.Add(nameof(this.CloudsConvertToMetric)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the undetermined layer altitude text.
    /// </summary>
    public string? UndeterminedLayerAltitudeText
    {
        get => this.undeterminedLayerAltitudeText;
        set
        {
            this.RaiseAndSetIfChanged(ref this.undeterminedLayerAltitudeText, value);
            if (!this.initializedProperties.Add(nameof(this.UndeterminedLayerAltitudeText)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the undetermined layer altitude voice.
    /// </summary>
    public string? UndeterminedLayerAltitudeVoice
    {
        get => this.undeterminedLayerAltitudeVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref this.undeterminedLayerAltitudeVoice, value);
            if (!this.initializedProperties.Add(nameof(this.UndeterminedLayerAltitudeVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether cloud height altitude is in hundreds.
    /// </summary>
    public bool CloudHeightAltitudeInHundreds
    {
        get => this.cloudHeightAltitudeInHundreds;
        set
        {
            this.RaiseAndSetIfChanged(ref this.cloudHeightAltitudeInHundreds, value);
            if (!this.initializedProperties.Add(nameof(this.CloudHeightAltitudeInHundreds)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use plus prefix for temperature.
    /// </summary>
    public bool TemperatureUsePlusPrefix
    {
        get => this.temperatureUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref this.temperatureUsePlusPrefix, value);
            if (!this.initializedProperties.Add(nameof(this.TemperatureUsePlusPrefix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to speak leading zero for temperature.
    /// </summary>
    public bool TemperatureSpeakLeadingZero
    {
        get => this.temperatureSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this.temperatureSpeakLeadingZero, value);
            if (!this.initializedProperties.Add(nameof(this.TemperatureSpeakLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use plus prefix for dewpoint.
    /// </summary>
    public bool DewpointUsePlusPrefix
    {
        get => this.dewpointUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref this.dewpointUsePlusPrefix, value);
            if (!this.initializedProperties.Add(nameof(this.DewpointUsePlusPrefix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to speak leading zero for dewpoint.
    /// </summary>
    public bool DewpointSpeakLeadingZero
    {
        get => this.dewpointSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this.dewpointSpeakLeadingZero, value);
            if (!this.initializedProperties.Add(nameof(this.DewpointSpeakLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to speak decimal for altimeter.
    /// </summary>
    public bool AltimeterSpeakDecimal
    {
        get => this.altimeterSpeakDecimal;
        set
        {
            this.RaiseAndSetIfChanged(ref this.altimeterSpeakDecimal, value);
            if (!this.initializedProperties.Add(nameof(this.AltimeterSpeakDecimal)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-include closing statement.
    /// </summary>
    public bool ClosingStatementAutoIncludeClosingStatement
    {
        get => this.closingStatementAutoIncludeClosingStatement;
        set
        {
            this.RaiseAndSetIfChanged(ref this.closingStatementAutoIncludeClosingStatement, value);
            if (!this.initializedProperties.Add(nameof(this.ClosingStatementAutoIncludeClosingStatement)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the transition levels.
    /// </summary>
    public ObservableCollection<TransitionLevelMeta>? TransitionLevels
    {
        get => this.transitionLevelMetas;
        set
        {
            this.RaiseAndSetIfChanged(ref this.transitionLevelMetas, value);
            if (!this.initializedProperties.Add(nameof(this.TransitionLevels)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the transition level text template.
    /// </summary>
    public string? TransitionLevelTextTemplate
    {
        get => this.transitionLevelTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.transitionLevelTextTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.TransitionLevelTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the transition level voice template.
    /// </summary>
    public string? TransitionLevelVoiceTemplate
    {
        get => this.transitionLevelVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this.transitionLevelVoiceTemplate, value);
            if (!this.initializedProperties.Add(nameof(this.TransitionLevelVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the present weather types.
    /// </summary>
    public ObservableCollection<PresentWeatherMeta>? PresentWeatherTypes
    {
        get => this.presentWeatherTypes;
        set => this.RaiseAndSetIfChanged(ref this.presentWeatherTypes, value);
    }

    /// <summary>
    /// Gets or sets the cloud types.
    /// </summary>
    public ObservableCollection<CloudTypeMeta>? CloudTypes
    {
        get => this.cloudTypes;
        set => this.RaiseAndSetIfChanged(ref this.cloudTypes, value);
    }

    /// <summary>
    /// Gets or sets the convective cloud types.
    /// </summary>
    public ObservableCollection<ConvectiveCloudTypeMeta>? ConvectiveCloudTypes
    {
        get => this.convectiveCloudTypes;
        set => this.RaiseAndSetIfChanged(ref this.convectiveCloudTypes, value);
    }

    /// <summary>
    /// Gets or sets the contraction completion data.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => this.contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this.contractionCompletionData, value);
    }

    /// <summary>
    /// Applies pending, unsaved changes.
    /// </summary>
    /// <returns>A value indicating whether changes are applied.</returns>
    public bool ApplyChanges()
    {
        if (this.SelectedStation == null)
        {
            return true;
        }

        this.ClearAllErrors();

        if (!string.IsNullOrEmpty(this.RoutineObservationTime))
        {
            var observationTimes = new List<int>();
            foreach (var interval in this.RoutineObservationTime.Split(','))
            {
                if (int.TryParse(interval, out var value))
                {
                    if (value < 0 || value > 59)
                    {
                        this.RaiseError(
                            nameof(this.RoutineObservationTime),
                            "Invalid routine observation time. Time values must between 0 and 59.");
                        return false;
                    }

                    if (observationTimes.Contains(value))
                    {
                        this.RaiseError(
                            nameof(this.RoutineObservationTime),
                            "Duplicate routine observation time values.");
                        return false;
                    }

                    observationTimes.Add(value);
                }
            }

            this.SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = observationTimes;
        }
        else
        {
            this.SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = null;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero != this.SpeakWindSpeedLeadingZero)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero = this.SpeakWindSpeedLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled != this.MagneticVariationEnabled)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = this.MagneticVariationEnabled;
        }

        if (int.TryParse(this.MagneticVariationValue, out var magneticVariation))
        {
            if (this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees != magneticVariation)
            {
                this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = magneticVariation;
            }
        }
        else
        {
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = false;
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = 0;
        }

        if (this.SelectedStation.AtisFormat.ObservationTime.Template.Text != this.ObservationTimeTextTemplate)
        {
            this.SelectedStation.AtisFormat.ObservationTime.Template.Text = this.ObservationTimeTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.ObservationTime.Template.Voice != this.ObservationTimeVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.ObservationTime.Template.Voice = this.ObservationTimeVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text != this.StandardWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text = this.StandardWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice != this.StandardWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice = this.StandardWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text != this.StandardGustWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text = this.StandardGustWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice !=
            this.StandardGustWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice =
                this.StandardGustWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text != this.VariableWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text = this.VariableWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice != this.VariableWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice = this.VariableWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text != this.VariableGustWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text = this.VariableGustWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice !=
            this.VariableGustWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice =
                this.VariableGustWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text !=
            this.VariableDirectionWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text =
                this.VariableDirectionWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice !=
            this.VariableDirectionWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice =
                this.VariableDirectionWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text != this.CalmWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text = this.CalmWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice != this.CalmWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice = this.CalmWindVoiceTemplate;
        }

        if (int.TryParse(this.CalmWindSpeed, out var speed))
        {
            if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed != speed)
            {
                this.SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed = speed;
            }
        }

        if (this.SelectedStation.AtisFormat.Visibility.Template.Text != this.VisibilityTextTemplate)
        {
            this.SelectedStation.AtisFormat.Visibility.Template.Text = this.VisibilityTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Visibility.Template.Voice != this.VisibilityVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Visibility.Template.Voice = this.VisibilityVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Template.Text != this.PresentWeatherTextTemplate)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Template.Text = this.PresentWeatherTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Template.Voice != this.PresentWeatherVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Template.Voice = this.PresentWeatherVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.RecentWeather.Template.Text != this.RecentWeatherTextTemplate)
        {
            this.SelectedStation.AtisFormat.RecentWeather.Template.Text = this.RecentWeatherTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.RecentWeather.Template.Voice != this.RecentWeatherVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.RecentWeather.Template.Voice = this.RecentWeatherVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Clouds.Template.Text != this.CloudsTextTemplate)
        {
            this.SelectedStation.AtisFormat.Clouds.Template.Text = this.CloudsTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Clouds.Template.Voice != this.CloudsVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Clouds.Template.Voice = this.CloudsVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Temperature.Template.Text != this.TemperatureTextTemplate)
        {
            this.SelectedStation.AtisFormat.Temperature.Template.Text = this.TemperatureTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Temperature.Template.Voice != this.TemperatureVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Temperature.Template.Voice = this.TemperatureVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.Template.Text != this.DewpointTextTemplate)
        {
            this.SelectedStation.AtisFormat.Dewpoint.Template.Text = this.DewpointTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.Template.Voice != this.DewpointVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Dewpoint.Template.Voice = this.DewpointVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.Template.Text != this.AltimeterTextTemplate)
        {
            this.SelectedStation.AtisFormat.Altimeter.Template.Text = this.AltimeterTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.Template.Voice != this.AltimeterVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Altimeter.Template.Voice = this.AltimeterVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.Template.Text != this.ClosingStatementTextTemplate)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.Template.Text = this.ClosingStatementTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.Template.Voice != this.ClosingStatementVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.Template.Voice = this.ClosingStatementVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Visibility.North != this.VisibilityNorth)
        {
            this.SelectedStation.AtisFormat.Visibility.North = this.VisibilityNorth ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.NorthEast != this.VisibilityNorthEast)
        {
            this.SelectedStation.AtisFormat.Visibility.NorthEast = this.VisibilityNorthEast ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.East != this.VisibilityEast)
        {
            this.SelectedStation.AtisFormat.Visibility.East = this.VisibilityEast ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.SouthEast != this.VisibilitySouthEast)
        {
            this.SelectedStation.AtisFormat.Visibility.SouthEast = this.VisibilitySouthEast ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.South != this.VisibilitySouth)
        {
            this.SelectedStation.AtisFormat.Visibility.South = this.VisibilitySouth ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.SouthWest != this.VisibilitySouthWest)
        {
            this.SelectedStation.AtisFormat.Visibility.SouthWest = this.VisibilitySouthWest ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.West != this.VisibilityWest)
        {
            this.SelectedStation.AtisFormat.Visibility.West = this.VisibilityWest ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.NorthWest != this.VisibilityNorthWest)
        {
            this.SelectedStation.AtisFormat.Visibility.NorthWest = this.VisibilityNorthWest ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice !=
            this.VisibilityUnlimitedVisibilityVoice)
        {
            this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice =
                this.VisibilityUnlimitedVisibilityVoice ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText !=
            this.VisibilityUnlimitedVisibilityText)
        {
            this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText =
                this.VisibilityUnlimitedVisibilityText ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix !=
            this.VisibilityIncludeVisibilitySuffix)
        {
            this.SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix = this.VisibilityIncludeVisibilitySuffix;
        }

        if (this.SelectedStation.AtisFormat.Visibility.MetersCutoff != this.VisibilityMetersCutoff)
        {
            this.SelectedStation.AtisFormat.Visibility.MetersCutoff = this.VisibilityMetersCutoff;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.LightIntensity != this.PresentWeatherLightIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.LightIntensity =
                this.PresentWeatherLightIntensity ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.ModerateIntensity != this.PresentWeatherModerateIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.ModerateIntensity =
                this.PresentWeatherModerateIntensity ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.HeavyIntensity != this.PresentWeatherHeavyIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.HeavyIntensity =
                this.PresentWeatherHeavyIntensity ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Vicinity != this.PresentWeatherVicinity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Vicinity = this.PresentWeatherVicinity ?? string.Empty;
        }

        if (this.PresentWeatherTypes != null && this.SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes !=
            this.PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken)))
        {
            this.SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes = this.PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken));
        }

        if (this.SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer != this.CloudsIdentifyCeilingLayer)
        {
            this.SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer = this.CloudsIdentifyCeilingLayer;
        }

        if (this.SelectedStation.AtisFormat.Clouds.ConvertToMetric != this.CloudsConvertToMetric)
        {
            this.SelectedStation.AtisFormat.Clouds.ConvertToMetric = this.CloudsConvertToMetric;
        }

        if (this.SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds != this.CloudHeightAltitudeInHundreds)
        {
            this.SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds = this.CloudHeightAltitudeInHundreds;
        }

        if (this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text != this.UndeterminedLayerAltitudeText)
        {
            this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text =
                this.UndeterminedLayerAltitudeText ?? string.Empty;
        }

        if (this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice !=
            this.UndeterminedLayerAltitudeVoice)
        {
            this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice =
                this.UndeterminedLayerAltitudeVoice ?? string.Empty;
        }

        if (this.CloudTypes != null && this.SelectedStation.AtisFormat.Clouds.Types != this.CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken)))
        {
            this.SelectedStation.AtisFormat.Clouds.Types = this.CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken));
        }

        if (this.ConvectiveCloudTypes != null && this.SelectedStation.AtisFormat.Clouds.ConvectiveTypes !=
            this.ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value))
        {
            this.SelectedStation.AtisFormat.Clouds.ConvectiveTypes =
                this.ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value);
        }

        if (this.SelectedStation.AtisFormat.Temperature.UsePlusPrefix != this.TemperatureUsePlusPrefix)
        {
            this.SelectedStation.AtisFormat.Temperature.UsePlusPrefix = this.TemperatureUsePlusPrefix;
        }

        if (this.SelectedStation.AtisFormat.Temperature.SpeakLeadingZero != this.TemperatureSpeakLeadingZero)
        {
            this.SelectedStation.AtisFormat.Temperature.SpeakLeadingZero = this.TemperatureSpeakLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix != this.DewpointUsePlusPrefix)
        {
            this.SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix = this.DewpointUsePlusPrefix;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero != this.DewpointSpeakLeadingZero)
        {
            this.SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero = this.DewpointSpeakLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.PronounceDecimal != this.AltimeterSpeakDecimal)
        {
            this.SelectedStation.AtisFormat.Altimeter.PronounceDecimal = this.AltimeterSpeakDecimal;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement !=
            this.ClosingStatementAutoIncludeClosingStatement)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement =
                this.ClosingStatementAutoIncludeClosingStatement;
        }

        if (this.SelectedStation.AtisFormat.TransitionLevel.Template.Text != this.TransitionLevelTextTemplate)
        {
            this.SelectedStation.AtisFormat.TransitionLevel.Template.Text = this.TransitionLevelTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.TransitionLevel.Template.Voice != this.TransitionLevelVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.TransitionLevel.Template.Voice = this.TransitionLevelVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Notams.Template.Text != this.NotamsTextTemplate)
        {
            this.SelectedStation.AtisFormat.Notams.Template.Text = this.NotamsTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Notams.Template.Voice != this.NotamsVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Notams.Template.Voice = this.NotamsVoiceTemplate;
        }

        if (this.HasErrors)
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

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedStation = station;

        this.FormattingOptions = [];
        if (station.IsFaaAtis)
        {
            this.FormattingOptions =
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
            this.FormattingOptions =
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

        this.SpeakWindSpeedLeadingZero = station.AtisFormat.SurfaceWind.SpeakLeadingZero;
        this.MagneticVariationEnabled = station.AtisFormat.SurfaceWind.MagneticVariation.Enabled;
        this.MagneticVariationValue = station.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees.ToString();
        this.RoutineObservationTime = string.Join(",", station.AtisFormat.ObservationTime.StandardUpdateTime ?? []);
        this.ObservationTimeTextTemplate = station.AtisFormat.ObservationTime.Template.Text;
        this.ObservationTimeVoiceTemplate = station.AtisFormat.ObservationTime.Template.Voice;
        this.StandardWindTextTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Text;
        this.StandardWindVoiceTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Voice;
        this.StandardGustWindTextTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Text;
        this.StandardGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Voice;
        this.VariableWindTextTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Text;
        this.VariableWindVoiceTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Voice;
        this.VariableGustWindTextTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Text;
        this.VariableGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Voice;
        this.VariableDirectionWindTextTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Text;
        this.VariableDirectionWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Voice;
        this.CalmWindTextTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Text;
        this.CalmWindVoiceTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Voice;
        this.CalmWindSpeed = station.AtisFormat.SurfaceWind.Calm.CalmWindSpeed.ToString();
        this.VisibilityTextTemplate = station.AtisFormat.Visibility.Template.Text;
        this.VisibilityVoiceTemplate = station.AtisFormat.Visibility.Template.Voice;
        this.PresentWeatherTextTemplate = station.AtisFormat.PresentWeather.Template.Text;
        this.PresentWeatherVoiceTemplate = station.AtisFormat.PresentWeather.Template.Voice;
        this.RecentWeatherVoiceTemplate = station.AtisFormat.RecentWeather.Template.Voice;
        this.RecentWeatherTextTemplate = station.AtisFormat.RecentWeather.Template.Text;
        this.CloudsTextTemplate = station.AtisFormat.Clouds.Template.Text;
        this.CloudsVoiceTemplate = station.AtisFormat.Clouds.Template.Voice;
        this.TemperatureTextTemplate = station.AtisFormat.Temperature.Template.Text;
        this.TemperatureVoiceTemplate = station.AtisFormat.Temperature.Template.Voice;
        this.DewpointTextTemplate = station.AtisFormat.Dewpoint.Template.Text;
        this.DewpointVoiceTemplate = station.AtisFormat.Dewpoint.Template.Voice;
        this.AltimeterTextTemplate = station.AtisFormat.Altimeter.Template.Text;
        this.AltimeterVoiceTemplate = station.AtisFormat.Altimeter.Template.Voice;
        this.ClosingStatementTextTemplate = station.AtisFormat.ClosingStatement.Template.Text;
        this.ClosingStatementVoiceTemplate = station.AtisFormat.ClosingStatement.Template.Voice;
        this.VisibilityNorth = station.AtisFormat.Visibility.North;
        this.VisibilityNorthEast = station.AtisFormat.Visibility.NorthEast;
        this.VisibilityEast = station.AtisFormat.Visibility.East;
        this.VisibilitySouthEast = station.AtisFormat.Visibility.SouthEast;
        this.VisibilitySouth = station.AtisFormat.Visibility.South;
        this.VisibilitySouthWest = station.AtisFormat.Visibility.SouthWest;
        this.VisibilityWest = station.AtisFormat.Visibility.West;
        this.VisibilityNorthWest = station.AtisFormat.Visibility.NorthWest;
        this.VisibilityUnlimitedVisibilityVoice = station.AtisFormat.Visibility.UnlimitedVisibilityVoice;
        this.VisibilityUnlimitedVisibilityText = station.AtisFormat.Visibility.UnlimitedVisibilityText;
        this.VisibilityIncludeVisibilitySuffix = station.AtisFormat.Visibility.IncludeVisibilitySuffix;
        this.VisibilityMetersCutoff = station.AtisFormat.Visibility.MetersCutoff;
        this.PresentWeatherLightIntensity = station.AtisFormat.PresentWeather.LightIntensity;
        this.PresentWeatherModerateIntensity = station.AtisFormat.PresentWeather.ModerateIntensity;
        this.PresentWeatherHeavyIntensity = station.AtisFormat.PresentWeather.HeavyIntensity;
        this.PresentWeatherVicinity = station.AtisFormat.PresentWeather.Vicinity;
        this.CloudsIdentifyCeilingLayer = station.AtisFormat.Clouds.IdentifyCeilingLayer;
        this.CloudsConvertToMetric = station.AtisFormat.Clouds.ConvertToMetric;
        this.CloudHeightAltitudeInHundreds = station.AtisFormat.Clouds.IsAltitudeInHundreds;
        this.UndeterminedLayerAltitudeText = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Text;
        this.UndeterminedLayerAltitudeVoice = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice;
        this.TemperatureUsePlusPrefix = station.AtisFormat.Temperature.UsePlusPrefix;
        this.TemperatureSpeakLeadingZero = station.AtisFormat.Temperature.SpeakLeadingZero;
        this.DewpointUsePlusPrefix = station.AtisFormat.Dewpoint.UsePlusPrefix;
        this.DewpointSpeakLeadingZero = station.AtisFormat.Dewpoint.SpeakLeadingZero;
        this.AltimeterSpeakDecimal = station.AtisFormat.Altimeter.PronounceDecimal;
        this.NotamsTextTemplate = station.AtisFormat.Notams.Template.Text;
        this.NotamsVoiceTemplate = station.AtisFormat.Notams.Template.Voice;
        this.ClosingStatementAutoIncludeClosingStatement =
            station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement;

        this.PresentWeatherTypes = [];
        foreach (var kvp in station.AtisFormat.PresentWeather.PresentWeatherTypes)
        {
            this.PresentWeatherTypes.Add(new PresentWeatherMeta(kvp.Key, kvp.Value.Text, kvp.Value.Spoken));
        }

        this.CloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.Types)
        {
            this.CloudTypes.Add(new CloudTypeMeta(item.Key, item.Value.Voice, item.Value.Text));
        }

        this.ConvectiveCloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.ConvectiveTypes)
        {
            this.ConvectiveCloudTypes.Add(new ConvectiveCloudTypeMeta(item.Key, item.Value));
        }

        this.TransitionLevelTextTemplate = station.AtisFormat.TransitionLevel.Template.Text;
        this.TransitionLevelVoiceTemplate = station.AtisFormat.TransitionLevel.Template.Voice;

        this.TransitionLevels = [];
        foreach (var item in station.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low))
        {
            this.TransitionLevels.Add(item);
        }

        this.ContractionCompletionData = [];
        foreach (var contraction in station.Contractions)
        {
            if (!string.IsNullOrEmpty(contraction.VariableName) && !string.IsNullOrEmpty(contraction.Voice))
            {
                this.ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }

        this.HasUnsavedChanges = false;
    }

    private async Task HandleAddTransitionLevel()
    {
        if (this.DialogOwner == null || this.SelectedStation == null)
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

            var dialog = this.windowFactory.CreateTransitionLevelDialog();
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

                        if (this.SelectedStation.AtisFormat.TransitionLevel.Values.Any(
                                x =>
                                    x.Low == intLow && x.High == intHigh))
                        {
                            context.RaiseError("QnhLow", "Duplicate transition level.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        this.SelectedStation.AtisFormat.TransitionLevel.Values.Add(
                            new TransitionLevelMeta(intLow, intHigh, intLevel));

                        this.TransitionLevels = [];
                        var sorted = this.SelectedStation.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low);
                        foreach (var item in sorted)
                        {
                            this.TransitionLevels.Add(item);
                        }

                        if (this.sessionManager.CurrentProfile != null)
                        {
                            this.profileRepository.Save(this.sessionManager.CurrentProfile);
                        }
                    }
                };
                await dialog.ShowDialog((Window)this.DialogOwner);
            }
        }
    }

    private async Task HandleDeleteTransitionLevel(TransitionLevelMeta? obj)
    {
        if (obj == null || this.TransitionLevels == null || this.DialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this.DialogOwner,
                "Are you sure you want to delete the selected transition level?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this.TransitionLevels.Remove(obj))
            {
                this.SelectedStation.AtisFormat.TransitionLevel.Values.Remove(obj);
                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
                }
            }
        }
    }

    private void HandleCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            this.HasUnsavedChanges = true;
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
