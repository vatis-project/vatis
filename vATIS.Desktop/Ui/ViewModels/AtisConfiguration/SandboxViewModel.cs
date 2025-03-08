// <copyright file="SandboxViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Represents the view model for the sandbox environment.
/// </summary>
public class SandboxViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IAtisBuilder _atisBuilder;
    private readonly IMetarRepository _metarRepository;
    private readonly Random _random = new();
    private readonly MetarDecoder _metarDecoder = new();
    private readonly CompositeDisposable _disposables = [];
    private CancellationTokenSource _cancellationToken;
    private ObservableCollection<AtisPreset>? _presets;
    private AtisPreset? _selectedPreset;
    private AtisStation? _selectedStation;
    private AtisPreset? _previousPreset;
    private int _notamFreeTextOffset;
    private int _airportConditionsFreeTextOffset;
    private string? _sandboxMetar;
    private bool _hasUnsavedAirportConditions;
    private bool _hasUnsavedNotams;
    private TextDocument? _airportConditionsTextDocument = new();
    private TextDocument? _notamsTextDocument = new();
    private TextDocument _textAtisTextDocument = new();
    private TextDocument _voiceAtisTextDocument = new();
    private List<ICompletionData> _contractionCompletionData = [];
    private bool _isSandboxPlaybackActive;
    private AtisBuilderVoiceAtisResponse? _atisBuilderVoiceResponse;
    private string? _previousFreeTextNotams;
    private string? _previousFreeTextAirportConditions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SandboxViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">An instance of <see cref="IWindowFactory"/> used for creating application windows.</param>
    /// <param name="atisBuilder">An instance of <see cref="IAtisBuilder"/> used for building ATIS.</param>
    /// <param name="metarRepository">An instance of <see cref="IMetarRepository"/> used for accessing METAR data.</param>
    /// <param name="profileRepository">An instance of <see cref="IProfileRepository"/> used for managing user profiles.</param>
    /// <param name="sessionManager">An instance of <see cref="ISessionManager"/> used for managing sessions.</param>
    public SandboxViewModel(IWindowFactory windowFactory, IAtisBuilder atisBuilder, IMetarRepository metarRepository,
        IProfileRepository profileRepository, ISessionManager sessionManager)
    {
        _windowFactory = windowFactory;
        _atisBuilder = atisBuilder;
        _metarRepository = metarRepository;
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;
        _cancellationToken = new CancellationTokenSource();

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        FetchSandboxMetarCommand = ReactiveCommand.CreateFromTask(HandleFetchSandboxMetar);
        SelectedPresetChangedCommand = ReactiveCommand.CreateFromTask<AtisPreset>(HandleSelectedPresetChanged);
        OpenStaticAirportConditionsDialogCommand =
            ReactiveCommand.CreateFromTask(HandleOpenStaticAirportConditionsDialog);
        OpenStaticNotamsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenStaticNotamsDialog);
        SaveAirportConditionsTextCommand = ReactiveCommand.Create(HandleSaveAirportConditionsText);
        SaveNotamsTextCommand = ReactiveCommand.Create(HandleSaveNotamsText);

        var canRefreshAtis = this.WhenAnyValue(
            x => x.IsSandboxPlaybackActive,
            x => x.SandboxMetar,
            x => x.SelectedPreset,
            (playback, metar, preset) => playback == false && metar != null && preset != null);
        RefreshSandboxAtisCommand = ReactiveCommand.CreateFromTask(HandleRefreshSandboxAtis, canRefreshAtis);

        var canPlaySandboxAtis = this.WhenAnyValue(
            x => x.AtisBuilderVoiceResponse,
            (resp) => resp?.AudioBytes != null);
        PlaySandboxAtisCommand = ReactiveCommand.CreateFromTask(HandlePlaySandboxAtis, canPlaySandboxAtis);

        ReadOnlyAirportConditions = new TextSegmentCollection<TextSegment>(AirportConditionsTextDocument);
        ReadOnlyNotams = new TextSegmentCollection<TextSegment>(NotamsTextDocument);

        _disposables.Add(EventBus.Instance.Subscribe<StationPresetsChanged>(evt =>
        {
            if (evt.Id == SelectedStation?.Id)
            {
                Presets = new ObservableCollection<AtisPreset>(SelectedStation.Presets);
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<ContractionsUpdated>(evt =>
        {
            if (evt.StationId == SelectedStation?.Id)
            {
                LoadContractionData();
            }
        }));
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
    /// Gets the command used to fetch the sandbox METAR.
    /// </summary>
    public ReactiveCommand<Unit, Unit> FetchSandboxMetarCommand { get; }

    /// <summary>
    /// Gets the command executed when the selected preset changes.
    /// </summary>
    public ReactiveCommand<AtisPreset, Unit> SelectedPresetChangedCommand { get; }

    /// <summary>
    /// Gets the command used to open the static airport conditions dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenStaticAirportConditionsDialogCommand { get; }

    /// <summary>
    /// Gets the command used to open the static NOTAMs dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenStaticNotamsDialogCommand { get; }

    /// <summary>
    /// Gets the command used to save the airport conditions text.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveAirportConditionsTextCommand { get; }

    /// <summary>
    /// Gets the command used to save the NOTAMs text.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveNotamsTextCommand { get; }

    /// <summary>
    /// Gets the command used to refresh the sandbox ATIS.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshSandboxAtisCommand { get; }

    /// <summary>
    /// Gets the command used to play the sandbox ATIS.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PlaySandboxAtisCommand { get; }

    /// <summary>
    /// Gets or sets the collection of ATIS presets.
    /// </summary>
    public ObservableCollection<AtisPreset>? Presets
    {
        get => _presets;
        set => this.RaiseAndSetIfChanged(ref _presets, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
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
    /// Gets or sets the sandbox METAR.
    /// </summary>
    public string? SandboxMetar
    {
        get => _sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref _sandboxMetar, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved airport conditions.
    /// </summary>
    public bool HasUnsavedAirportConditions
    {
        get => _hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedAirportConditions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved NOTAMs.
    /// </summary>
    public bool HasUnsavedNotams
    {
        get => _hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedNotams, value);
    }

    /// <summary>
    /// Gets the free text representation of the airport conditions.
    /// </summary>
    public string? AirportConditionsFreeText => AirportConditionsTextDocument?.Text;

    /// <summary>
    /// Gets or sets the airport conditions text document.
    /// </summary>
    public TextDocument? AirportConditionsTextDocument
    {
        get => _airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _airportConditionsTextDocument, value);
    }

    /// <summary>
    /// Gets the free-text representation of the NOTAMs from the text document.
    /// </summary>
    public string? NotamsFreeText => _notamsTextDocument?.Text;

    /// <summary>
    /// Gets or sets the NOTAMs text document.
    /// </summary>
    public TextDocument? NotamsTextDocument
    {
        get => _notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _notamsTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the text ATIS text document.
    /// </summary>
    public TextDocument TextAtisTextDocument
    {
        get => _textAtisTextDocument;
        set => this.RaiseAndSetIfChanged(ref _textAtisTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the text ATIS text document.
    /// </summary>
    public TextDocument VoiceAtisTextDocument
    {
        get => _voiceAtisTextDocument;
        set => this.RaiseAndSetIfChanged(ref _voiceAtisTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the collection of read-only airport condition text segments.
    /// </summary>
    public TextSegmentCollection<TextSegment> ReadOnlyAirportConditions { get; set; }

    /// <summary>
    /// Gets or sets the collection of read-only NOTAM text segments.
    /// </summary>
    public TextSegmentCollection<TextSegment> ReadOnlyNotams { get; set; }

    /// <summary>
    /// Gets or sets the contraction completion data.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sandbox playback is active.
    /// </summary>
    public bool IsSandboxPlaybackActive
    {
        get => _isSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref _isSandboxPlaybackActive, value);
    }

    /// <summary>
    /// Gets or sets the ATIS builder response.
    /// </summary>
    public AtisBuilderVoiceAtisResponse? AtisBuilderVoiceResponse
    {
        get => _atisBuilderVoiceResponse;
        set => this.RaiseAndSetIfChanged(ref _atisBuilderVoiceResponse, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Applies sandbox configuration by verifying the state of unsaved data and stopping sandbox playback if required.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if the configuration is applied successfully; <c>false</c> if there are unsaved airport conditions or NOTAMs.
    /// </returns>
    public bool ApplyConfig()
    {
        if (HasUnsavedNotams || HasUnsavedAirportConditions)
            return false;

        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();

        return true;
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
            return;

        SelectedPreset = null;
        SelectedStation = station;
        Presets = new ObservableCollection<AtisPreset>(station.Presets);
        SandboxMetar = "";
        HasUnsavedAirportConditions = false;
        HasUnsavedNotams = false;
        TextAtisTextDocument.Text = "";
        VoiceAtisTextDocument.Text = "";

        if (AirportConditionsTextDocument != null)
        {
            AirportConditionsTextDocument.Text = "";
        }

        if (NotamsTextDocument != null)
        {
            NotamsTextDocument.Text = "";
        }

        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();
        LoadContractionData();
    }

    private async Task HandlePlaySandboxAtis(CancellationToken token)
    {
        if (SelectedStation == null || SelectedPreset == null || AtisBuilderVoiceResponse == null)
            return;

        await _cancellationToken.CancelAsync();
        _cancellationToken.Dispose();
        _cancellationToken = new CancellationTokenSource();

        if (AtisBuilderVoiceResponse.AudioBytes == null)
            return;

        if (!IsSandboxPlaybackActive)
        {
            IsSandboxPlaybackActive = NativeAudio.StartBufferPlayback(AtisBuilderVoiceResponse.AudioBytes,
                AtisBuilderVoiceResponse.AudioBytes.Length);
        }
        else
        {
            IsSandboxPlaybackActive = false;
            NativeAudio.StopBufferPlayback();
        }
    }

    private async Task HandleRefreshSandboxAtis()
    {
        try
        {
            if (SelectedStation == null || SelectedPreset == null)
                return;

            NativeAudio.StopBufferPlayback();
            AtisBuilderVoiceResponse = null;

            await _cancellationToken.CancelAsync();
            _cancellationToken.Dispose();
            _cancellationToken = new CancellationTokenSource();

            TextAtisTextDocument.Text = "Loading...";
            VoiceAtisTextDocument.Text = "Loading...";

            var randomLetter =
                (char)_random.Next(SelectedStation.CodeRange.Low + SelectedStation.CodeRange.High + 1);

            if (randomLetter is < 'A' or > 'Z')
                randomLetter = 'A';

            if (SandboxMetar != null)
            {
                var decodedMetar = _metarDecoder.ParseNotStrict(SandboxMetar);
                var textAtis = await _atisBuilder.BuildTextAtis(SelectedStation, SelectedPreset, randomLetter,
                    decodedMetar, _cancellationToken.Token);
                AtisBuilderVoiceResponse = await _atisBuilder.BuildVoiceAtis(SelectedStation, SelectedPreset,
                    randomLetter,
                    decodedMetar, _cancellationToken.Token, true);
                TextAtisTextDocument.Text = textAtis?.ToUpperInvariant() ?? "";
                VoiceAtisTextDocument.Text = AtisBuilderVoiceResponse.SpokenText?.ToUpperInvariant() ?? "";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh sandbox ATIS for station {StationId} {Identifier} with preset {PresetId}",
                SelectedStation?.Id, SelectedStation?.Identifier, SelectedPreset?.Id);
            TextAtisTextDocument.Text = "Error: " + ex.Message;
            VoiceAtisTextDocument.Text = "";
        }
    }

    private void HandleSaveNotamsText()
    {
        if (SelectedPreset == null)
            return;

        string? freeText;

        var readonlySegment = ReadOnlyNotams.FirstSegment;
        if (readonlySegment != null)
        {
            freeText = readonlySegment.StartOffset > 0
                ? NotamsTextDocument?.Text[..readonlySegment.StartOffset]
                : NotamsTextDocument?.Text[readonlySegment.Length..];
        }
        else
        {
            freeText = NotamsTextDocument?.Text;
        }

        SelectedPreset.Notams = freeText?.Trim();

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedNotams = false;
    }

    private async Task HandleOpenStaticNotamsDialog()
    {
        if (DialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        if (SelectedStation == null)
            return;

        var dlg = _windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(SelectedStation.NotamDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;
            viewModel.IncludeBeforeFreeText = SelectedStation.NotamsBeforeFreeText;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                SelectedStation.NotamsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    SelectedStation.NotamDefinitions.Clear();
                    SelectedStation.NotamDefinitions.AddRange(changes);
                    if (_sessionManager.CurrentProfile != null)
                        _profileRepository.Save(_sessionManager.CurrentProfile);
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                SelectedStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    SelectedStation.NotamDefinitions.Add(item);
                }

                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        // Update the free-form text area after the dialog is closed
        PopulateNotams();
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedPreset == null)
            return;

        string? freeText;

        var readonlySegment = ReadOnlyAirportConditions.FirstSegment;
        if (readonlySegment != null)
        {
            freeText = readonlySegment.StartOffset > 0
                ? AirportConditionsTextDocument?.Text[..readonlySegment.StartOffset]
                : AirportConditionsTextDocument?.Text[readonlySegment.Length..];
        }
        else
        {
            freeText = AirportConditionsTextDocument?.Text;
        }

        SelectedPreset.AirportConditions = freeText?.Trim();

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedAirportConditions = false;
    }

    private async Task HandleOpenStaticAirportConditionsDialog()
    {
        if (DialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        if (SelectedStation == null)
            return;

        var dlg = _windowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions =
                new ObservableCollection<StaticDefinition>(SelectedStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;
            viewModel.IncludeBeforeFreeText = SelectedStation.AirportConditionsBeforeFreeText;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                SelectedStation.AirportConditionsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    SelectedStation.AirportConditionDefinitions.Clear();
                    SelectedStation.AirportConditionDefinitions.AddRange(changes);
                    if (_sessionManager.CurrentProfile != null)
                        _profileRepository.Save(_sessionManager.CurrentProfile);
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                SelectedStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    SelectedStation.AirportConditionDefinitions.Add(item);
                }

                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        // Update the free-form text area after the dialog is closed
        PopulateAirportConditions();
    }

    private async Task HandleSelectedPresetChanged(AtisPreset? preset)
    {
        if (preset == null || DialogOwner == null)
            return;

        if (preset != _previousPreset)
        {
            if (HasUnsavedNotams || HasUnsavedAirportConditions)
            {
                if (await MessageBox.ShowDialog((Window)DialogOwner,
                        "You have unsaved Airport Conditions or NOTAMs. Would you like to save them first?",
                        "Confirm", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                {
                    SaveNotamsTextCommand.Execute().Subscribe();
                    SaveAirportConditionsTextCommand.Execute().Subscribe();
                }
            }

            SelectedPreset = preset;
            _previousPreset = preset;

            PopulateAirportConditions(presetChanged: true);
            PopulateNotams(presetChanged: true);

            HasUnsavedNotams = false;
            HasUnsavedAirportConditions = false;
        }
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (SelectedStation == null || string.IsNullOrEmpty(SelectedStation.Identifier))
            return;

        var metar = await _metarRepository.GetMetar(SelectedStation.Identifier, monitor: false,
            triggerMessageBus: false);
        SandboxMetar = metar?.RawMetar;
    }

    private void LoadContractionData()
    {
        if (SelectedStation == null)
            return;

        ContractionCompletionData.Clear();

        foreach (var contraction in SelectedStation.Contractions.ToList())
        {
            if (contraction is { VariableName: not null, Voice: not null })
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
        }
    }

    private void PopulateNotams(bool presetChanged = false)
    {
        if (NotamsTextDocument == null)
            return;

        if (SelectedStation == null || SelectedPreset == null)
            return;

        // Retrieve and sort enabled static NOTAM definitions by their ordinal value.
        var staticDefinitions = SelectedStation.NotamDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Get user entered free-text before refreshing, in case the user entered new text, and it's not saved.
        if (!presetChanged)
        {
            var readonlySegment = ReadOnlyNotams.FirstSegment;
            if (readonlySegment != null)
            {
                _previousFreeTextNotams = readonlySegment.StartOffset > 0
                    ? NotamsTextDocument.Text[..readonlySegment.StartOffset]
                    : NotamsTextDocument.Text[readonlySegment.Length..];
            }
        }
        else
        {
            _previousFreeTextNotams = "";
        }

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyNotams.Clear();

        // Start with an empty document.
        NotamsTextDocument.Text = "";

        // Reset offset
        _notamFreeTextOffset = 0;

        var staticDefinitionsString = string.Join(". ", staticDefinitions.Select(s => s.Text.TrimEnd('.'))) + ". ";

        // Insert static definitions before free-text
        if (SelectedStation.NotamsBeforeFreeText)
        {
            if (staticDefinitions.Count > 0)
            {
                // Insert static definitions
                NotamsTextDocument.Insert(0, staticDefinitionsString);

                // Mark static definitions segment as readonly
                ReadOnlyNotams.Add(new TextSegment
                {
                    StartOffset = 0, Length = staticDefinitionsString.Length
                });

                _notamFreeTextOffset = staticDefinitionsString.Length;
            }

            // Insert free-text after static definitions
            if (!string.IsNullOrEmpty(_previousFreeTextNotams))
            {
                NotamsTextDocument.Insert(_notamFreeTextOffset, _previousFreeTextNotams.Trim());
            }
            else if (!string.IsNullOrEmpty(SelectedPreset.Notams))
            {
                NotamsTextDocument.Insert(_notamFreeTextOffset, SelectedPreset.Notams);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_previousFreeTextNotams))
            {
                NotamsTextDocument.Insert(0, _previousFreeTextNotams.Trim() + " ");
                _notamFreeTextOffset = _previousFreeTextNotams.Trim().Length + 1;
            }
            else if (!string.IsNullOrEmpty(SelectedPreset.Notams))
            {
                NotamsTextDocument.Insert(0, SelectedPreset.Notams.Trim() + " ");
                _notamFreeTextOffset = SelectedPreset.Notams.Trim().Length + 1;
            }

            // Insert static definitions after free-text
            if (staticDefinitions.Count > 0)
            {
                NotamsTextDocument.Insert(_notamFreeTextOffset, staticDefinitionsString);

                // Mark static definitions segment as readonly
                ReadOnlyNotams.Add(new TextSegment
                {
                    StartOffset = _notamFreeTextOffset, Length = staticDefinitionsString.Length
                });
            }
        }
    }

    private void PopulateAirportConditions(bool presetChanged = false)
    {
        if (AirportConditionsTextDocument == null || SelectedStation == null || SelectedPreset == null)
            return;

        // Retrieve and sort enabled static airport conditions by their ordinal value.
        var staticDefinitions = SelectedStation.AirportConditionDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Get user entered free-text before refreshing, in case the user entered new text, and it's not saved.
        if (!presetChanged)
        {
            var readonlySegment = ReadOnlyAirportConditions.FirstSegment;
            if (readonlySegment != null)
            {
                _previousFreeTextAirportConditions = readonlySegment.StartOffset > 0
                    ? AirportConditionsTextDocument.Text[..readonlySegment.StartOffset]
                    : AirportConditionsTextDocument.Text[readonlySegment.Length..];
            }
        }
        else
        {
            _previousFreeTextAirportConditions = "";
        }

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyAirportConditions.Clear();

        // Start with an empty document.
        AirportConditionsTextDocument.Text = "";

        // Reset offset
        _airportConditionsFreeTextOffset = 0;

        var staticDefinitionsString = string.Join(". ", staticDefinitions.Select(s => s.Text.TrimEnd('.'))) + ". ";

        // Insert static definitions before free-text
        if (SelectedStation.AirportConditionsBeforeFreeText)
        {
            if (staticDefinitions.Count > 0)
            {
                // Insert static definitions
                AirportConditionsTextDocument.Insert(0, staticDefinitionsString);

                // Mark static definitions segment as readonly
                ReadOnlyAirportConditions.Add(new TextSegment
                {
                    StartOffset = 0, Length = staticDefinitionsString.Length
                });

                _airportConditionsFreeTextOffset = staticDefinitionsString.Length;
            }

            // Insert free-text after static definitions
            if (!string.IsNullOrEmpty(_previousFreeTextAirportConditions))
            {
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset, _previousFreeTextAirportConditions.Trim());
            }
            else if (!string.IsNullOrEmpty(SelectedPreset.AirportConditions))
            {
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset, SelectedPreset.AirportConditions);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_previousFreeTextAirportConditions))
            {
                AirportConditionsTextDocument.Insert(0, _previousFreeTextAirportConditions.Trim() + " ");
                _airportConditionsFreeTextOffset = _previousFreeTextAirportConditions.Trim().Length + 1;
            }
            else if (!string.IsNullOrEmpty(SelectedPreset.AirportConditions))
            {
                AirportConditionsTextDocument.Insert(0, SelectedPreset.AirportConditions.Trim() + " ");
                _airportConditionsFreeTextOffset = SelectedPreset.AirportConditions.Trim().Length + 1;
            }

            // Insert static definitions after free-text
            if (staticDefinitions.Count > 0)
            {
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset, staticDefinitionsString);

                // Mark static definitions segment as readonly
                ReadOnlyAirportConditions.Add(new TextSegment
                {
                    StartOffset = _airportConditionsFreeTextOffset, Length = staticDefinitionsString.Length
                });
            }
        }
    }
}
