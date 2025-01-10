using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
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
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

public class SandboxViewModel : ReactiveViewModelBase
{
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IAtisBuilder _atisBuilder;
    private readonly IMetarRepository _metarRepository;
    private CancellationTokenSource _cancellationToken;
    private readonly Random _random = new();
    private readonly MetarDecoder _metarDecoder = new();

    public IDialogOwner? DialogOwner { get; set; }
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }
    public ReactiveCommand<Unit, Unit> FetchSandboxMetarCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectedPresetChangedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenStaticAirportConditionsDialogCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenStaticNotamsDialogCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAirportConditionsTextCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveNotamsTextCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshSandboxAtisCommand { get; }
    public ReactiveCommand<Unit, Unit> PlaySandboxAtisCommand { get; }

    #region Reactive Properties
    private ObservableCollection<AtisPreset>? _presets;
    public ObservableCollection<AtisPreset>? Presets
    {
        get => _presets;
        set => this.RaiseAndSetIfChanged(ref _presets, value);
    }

    private AtisPreset? _selectedPreset;
    public AtisPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
    }

    private AtisStation? _selectedStation;
    private AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    private string? _sandboxMetar;
    public string? SandboxMetar
    {
        get => _sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref _sandboxMetar, value);
    }

    private bool _hasUnsavedAirportConditions;
    public bool HasUnsavedAirportConditions
    {
        get => _hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedAirportConditions, value);
    }

    private bool _hasUnsavedNotams;
    public bool HasUnsavedNotams
    {
        get => _hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedNotams, value);
    }

    private string? AirportConditionsText
    {
        get => _airportConditionsTextDocument.Text ?? "";
        set => AirportConditionsTextDocument = new TextDocument(value);
    }

    private TextDocument _airportConditionsTextDocument = new();
    public TextDocument AirportConditionsTextDocument
    {
        get => _airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _airportConditionsTextDocument, value);
    }

    private string? NotamText
    {
        get => _notamsTextDocument.Text ?? "";
        set => NotamsTextDocument = new TextDocument(value);
    }

    private TextDocument _notamsTextDocument = new();
    public TextDocument NotamsTextDocument
    {
        get => _notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _notamsTextDocument, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    private string? _sandboxTextAtis;
    public string? SandboxTextAtis
    {
        get => _sandboxTextAtis;
        set => this.RaiseAndSetIfChanged(ref _sandboxTextAtis, value);
    }

    private string? _sandboxSpokenTextAtis;
    public string? SandboxSpokenTextAtis
    {
        get => _sandboxSpokenTextAtis;
        set => this.RaiseAndSetIfChanged(ref _sandboxSpokenTextAtis, value);
    }

    private bool _isSandboxPlaybackActive;
    public bool IsSandboxPlaybackActive
    {
        get => _isSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref _isSandboxPlaybackActive, value);
    }

    private AtisBuilderVoiceAtisResponse? _atisBuilderVoiceAtisResponse;
    private AtisBuilderVoiceAtisResponse? AtisBuilderVoiceAtisResponse
    {
        get => _atisBuilderVoiceAtisResponse;
        set => this.RaiseAndSetIfChanged(ref _atisBuilderVoiceAtisResponse, value);
    }
    #endregion

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
            x => x.AtisBuilderVoiceAtisResponse,
            (resp) => resp?.AudioBytes != null);
        PlaySandboxAtisCommand = ReactiveCommand.CreateFromTask(HandlePlaySandboxAtis, canPlaySandboxAtis);

        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(evt =>
        {
            if (evt.Id == SelectedStation?.Id)
            {
                Presets = new ObservableCollection<AtisPreset>(SelectedStation.Presets);
            }
        });
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
        AirportConditionsText = "";
        NotamText = "";
        SandboxTextAtis = "";
        SandboxSpokenTextAtis = "";
        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();
    }

    public bool ApplyConfig()
    {
        if (HasUnsavedNotams || HasUnsavedAirportConditions)
            return false;

        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();

        return true;
    }

    private async Task HandlePlaySandboxAtis(CancellationToken token)
    {
        if (SelectedStation == null || SelectedPreset == null || AtisBuilderVoiceAtisResponse == null)
            return;

        await _cancellationToken.CancelAsync();
        _cancellationToken.Dispose();
        _cancellationToken = new CancellationTokenSource();

        if (AtisBuilderVoiceAtisResponse.AudioBytes == null)
            return;

        if (!IsSandboxPlaybackActive)
        {
            IsSandboxPlaybackActive = NativeAudio.StartBufferPlayback(AtisBuilderVoiceAtisResponse.AudioBytes,
                AtisBuilderVoiceAtisResponse.AudioBytes.Length);
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
            AtisBuilderVoiceAtisResponse = null;

            await _cancellationToken.CancelAsync();
            _cancellationToken.Dispose();
            _cancellationToken = new CancellationTokenSource();

            SandboxTextAtis = "Loading...";
            SandboxSpokenTextAtis = "Loading...";

            var randomLetter =
                (char)_random.Next(SelectedStation.CodeRange.Low + SelectedStation.CodeRange.High + 1);

            if (randomLetter is < 'A' or > 'Z')
                randomLetter = 'A';

            SelectedPreset.AirportConditions = AirportConditionsText;
            SelectedPreset.Notams = NotamText;

            if (SandboxMetar != null)
            {
                var decodedMetar = mMetarDecoder.ParseNotStrict(SandboxMetar);
                var textAtis = await mAtisBuilder.BuildTextAtis(SelectedStation, SelectedPreset, randomLetter,
                    decodedMetar, mCancellationToken.Token);
                AtisBuilderVoiceAtisResponse = await mAtisBuilder.BuildVoiceAtis(SelectedStation, SelectedPreset, randomLetter,
                    decodedMetar, mCancellationToken.Token, true);
                SandboxTextAtis = textAtis?.ToUpperInvariant();
                SandboxSpokenTextAtis = AtisBuilderVoiceAtisResponse.SpokenText?.ToUpperInvariant();
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

        SelectedPreset.Notams = NotamText;

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
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedPreset == null)
            return;

        SelectedPreset.AirportConditions = AirportConditionsText;
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
    }


    private void HandleSelectedPresetChanged()
    {
        if (SelectedPreset == null)
            return;

        AirportConditionsText = SelectedPreset.AirportConditions?.ToUpperInvariant() ?? "";
        NotamText = SelectedPreset.Notams?.ToUpperInvariant() ?? "";
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (SelectedStation == null || string.IsNullOrEmpty(SelectedStation.Identifier))
            return;

        var metar = await _metarRepository.GetMetar(SelectedStation.Identifier, monitor: false,
            triggerMessageBus: false);
        SandboxMetar = metar?.RawMetar;
    }
}
