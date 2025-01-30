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
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
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
    private int _notamFreeTextOffset;
    private int _airportConditionsFreeTextOffset;
    private string? _sandboxMetar;
    private bool _hasUnsavedAirportConditions;
    private bool _hasUnsavedNotams;
    private TextDocument? _airportConditionsTextDocument = new();
    private TextDocument? _notamsTextDocument = new();
    private List<ICompletionData> _contractionCompletionData = [];
    private string? _sandboxTextAtis;
    private string? _sandboxSpokenTextAtis;
    private bool _isSandboxPlaybackActive;
    private AtisBuilderVoiceAtisResponse? _atisBuilderVoiceResponse;

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
        SelectedPresetChangedCommand = ReactiveCommand.Create(HandleSelectedPresetChanged);
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
    public ReactiveCommand<Unit, Unit> SelectedPresetChangedCommand { get; }

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
    /// Gets or sets the sandbox text ATIS.
    /// </summary>
    public string? SandboxTextAtis
    {
        get => _sandboxTextAtis;
        set => this.RaiseAndSetIfChanged(ref _sandboxTextAtis, value);
    }

    /// <summary>
    /// Gets or sets the sandbox spoken text ATIS.
    /// </summary>
    public string? SandboxSpokenTextAtis
    {
        get => _sandboxSpokenTextAtis;
        set => this.RaiseAndSetIfChanged(ref _sandboxSpokenTextAtis, value);
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
        SandboxTextAtis = "";
        SandboxSpokenTextAtis = "";
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

            SandboxTextAtis = "Loading...";
            SandboxSpokenTextAtis = "Loading...";

            var randomLetter =
                (char)_random.Next(SelectedStation.CodeRange.Low + SelectedStation.CodeRange.High + 1);

            if (randomLetter is < 'A' or > 'Z')
                randomLetter = 'A';

            SelectedPreset.AirportConditions = AirportConditionsFreeText?[_airportConditionsFreeTextOffset..];
            SelectedPreset.Notams = NotamsFreeText?[_notamFreeTextOffset..];

            if (SandboxMetar != null)
            {
                var decodedMetar = _metarDecoder.ParseNotStrict(SandboxMetar);
                var textAtis = await _atisBuilder.BuildTextAtis(SelectedStation, SelectedPreset, randomLetter,
                    decodedMetar, _cancellationToken.Token);
                AtisBuilderVoiceResponse = await _atisBuilder.BuildVoiceAtis(SelectedStation, SelectedPreset, randomLetter,
                    decodedMetar, _cancellationToken.Token, true);
                SandboxTextAtis = textAtis?.ToUpperInvariant();
                SandboxSpokenTextAtis = AtisBuilderVoiceResponse.SpokenText?.ToUpperInvariant();
            }
        }
        catch (Exception)
        {
            SandboxTextAtis = "";
            SandboxSpokenTextAtis = "";
            throw;
        }
    }

    private void HandleSaveNotamsText()
    {
        if (SelectedPreset == null)
            return;

        SelectedPreset.Notams = NotamsFreeText?[_notamFreeTextOffset..];

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

        SelectedPreset.AirportConditions = AirportConditionsFreeText?[_airportConditionsFreeTextOffset..];
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

    private void HandleSelectedPresetChanged()
    {
        if (SelectedPreset == null)
            return;

        PopulateAirportConditions();
        PopulateNotams();
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

    private void PopulateNotams()
    {
        if (NotamsTextDocument == null)
            return;

        if (SelectedStation == null)
            return;

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyNotams.Clear();

        // Retrieve and sort enabled static NOTAM definitions by their ordinal value.
        var staticDefinitions = SelectedStation.NotamDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        NotamsTextDocument.Text = "";

        // Reset offset
        _notamFreeTextOffset = 0;

        // If static NOTAM definitions exist, insert them into the document.
        if (staticDefinitions.Count > 0)
        {
            // Combine static NOTAM definitions into a single string, separated by periods.
            var staticDefinitionsString = string.Join(". ", staticDefinitions) + ". ";

            // Insert static NOTAM definitions at the beginning of the document.
            NotamsTextDocument.Insert(0, staticDefinitionsString);

            // Add the static NOTAM range to the read-only list to prevent modification.
            ReadOnlyNotams.Add(new TextSegment
            {
                StartOffset = 0,
                EndOffset = staticDefinitionsString.Length
            });

            // Update the starting index for the next insertion.
            _notamFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form NOTAM text after the static definitions (if any).
        if (!string.IsNullOrEmpty(SelectedPreset?.Notams))
        {
            NotamsTextDocument.Insert(_notamFreeTextOffset, SelectedPreset?.Notams);
        }
    }

    private void PopulateAirportConditions()
    {
        if (AirportConditionsTextDocument == null)
            return;

        if (SelectedStation == null)
            return;

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyAirportConditions.Clear();

        // Retrieve and sort enabled static airport conditions by their ordinal value.
        var staticDefinitions = SelectedStation.AirportConditionDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        AirportConditionsTextDocument.Text = "";

        // Reset offset
        _airportConditionsFreeTextOffset = 0;

        // If static airport conditions exist, insert them into the document.
        if (staticDefinitions.Count > 0)
        {
            // Combine static airport conditions into a single string, separated by periods.
            // A trailing space is added to ensure proper spacing between the static definitions
            // and the subsequent free-form text.
            var staticDefinitionsString = string.Join(". ", staticDefinitions) + ". ";

            // Insert static airport conditions at the beginning of the document.
            AirportConditionsTextDocument.Insert(0, staticDefinitionsString);

            // Add the static airport conditions to the read-only list to prevent modification.
            ReadOnlyAirportConditions.Add(new TextSegment
            {
                StartOffset = 0,
                EndOffset = staticDefinitionsString.Length
            });

            // Update the starting index for the next insertion.
            _airportConditionsFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form airport conditions after the static definitions (if any).
        if (!string.IsNullOrEmpty(SelectedPreset?.AirportConditions))
        {
            AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset,
                SelectedPreset?.AirportConditions);
        }
    }
}
