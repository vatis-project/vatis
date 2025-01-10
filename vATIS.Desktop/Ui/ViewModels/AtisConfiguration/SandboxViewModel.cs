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
    private readonly IAtisBuilder _atisBuilder;
    private readonly MetarDecoder _metarDecoder = new();
    private readonly IMetarRepository _metarRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly Random _random = new();
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private CancellationTokenSource _cancellationToken;

    public SandboxViewModel(
        IWindowFactory windowFactory,
        IAtisBuilder atisBuilder,
        IMetarRepository metarRepository,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        this._windowFactory = windowFactory;
        this._atisBuilder = atisBuilder;
        this._metarRepository = metarRepository;
        this._profileRepository = profileRepository;
        this._sessionManager = sessionManager;
        this._cancellationToken = new CancellationTokenSource();

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleAtisStationChanged);
        this.FetchSandboxMetarCommand = ReactiveCommand.CreateFromTask(this.HandleFetchSandboxMetar);
        this.SelectedPresetChangedCommand = ReactiveCommand.Create(this.HandleSelectedPresetChanged);
        this.OpenStaticAirportConditionsDialogCommand =
            ReactiveCommand.CreateFromTask(this.HandleOpenStaticAirportConditionsDialog);
        this.OpenStaticNotamsDialogCommand = ReactiveCommand.CreateFromTask(this.HandleOpenStaticNotamsDialog);
        this.SaveAirportConditionsTextCommand = ReactiveCommand.Create(this.HandleSaveAirportConditionsText);
        this.SaveNotamsTextCommand = ReactiveCommand.Create(this.HandleSaveNotamsText);

        var canRefreshAtis = this.WhenAnyValue(
            x => x.IsSandboxPlaybackActive,
            x => x.SandboxMetar,
            x => x.SelectedPreset,
            (playback, metar, preset) => playback == false && metar != null && preset != null);
        this.RefreshSandboxAtisCommand = ReactiveCommand.CreateFromTask(this.HandleRefreshSandboxAtis, canRefreshAtis);

        var canPlaySandboxAtis = this.WhenAnyValue(
            x => x.AtisBuilderResponse,
            resp => resp?.AudioBytes != null);
        this.PlaySandboxAtisCommand = ReactiveCommand.CreateFromTask(this.HandlePlaySandboxAtis, canPlaySandboxAtis);

        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(
            evt =>
            {
                if (evt.Id == this.SelectedStation?.Id)
                {
                    this.Presets = new ObservableCollection<AtisPreset>(this.SelectedStation.Presets);
                }
            });
    }

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

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedPreset = null;
        this.SelectedStation = station;
        this.Presets = new ObservableCollection<AtisPreset>(station.Presets);
        this.SandboxMetar = "";
        this.HasUnsavedAirportConditions = false;
        this.HasUnsavedNotams = false;
        this.AirportConditionsText = "";
        this.NotamText = "";
        this.SandboxTextAtis = "";
        this.SandboxSpokenTextAtis = "";
        this.IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();
    }

    public bool ApplyConfig()
    {
        if (this.HasUnsavedNotams || this.HasUnsavedAirportConditions)
        {
            return false;
        }

        this.IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();

        return true;
    }

    private async Task HandlePlaySandboxAtis(CancellationToken token)
    {
        if (this.SelectedStation == null || this.SelectedPreset == null || this.AtisBuilderResponse == null)
        {
            return;
        }

        await this._cancellationToken.CancelAsync();
        this._cancellationToken.Dispose();
        this._cancellationToken = new CancellationTokenSource();

        if (this.AtisBuilderResponse.AudioBytes == null)
        {
            return;
        }

        if (!this.IsSandboxPlaybackActive)
        {
            this.IsSandboxPlaybackActive = NativeAudio.StartBufferPlayback(
                this.AtisBuilderResponse.AudioBytes,
                this.AtisBuilderResponse.AudioBytes.Length);
        }
        else
        {
            this.IsSandboxPlaybackActive = false;
            NativeAudio.StopBufferPlayback();
        }
    }

    private async Task HandleRefreshSandboxAtis()
    {
        try
        {
            if (this.SelectedStation == null || this.SelectedPreset == null)
            {
                return;
            }

            NativeAudio.StopBufferPlayback();
            this.AtisBuilderResponse = null;

            await this._cancellationToken.CancelAsync();
            this._cancellationToken.Dispose();
            this._cancellationToken = new CancellationTokenSource();

            this.SandboxTextAtis = "Loading...";
            this.SandboxSpokenTextAtis = "Loading...";

            var randomLetter =
                (char)this._random.Next(this.SelectedStation.CodeRange.Low + this.SelectedStation.CodeRange.High + 1);

            if (randomLetter is < 'A' or > 'Z')
            {
                randomLetter = 'A';
            }

            this.SelectedPreset.AirportConditions = this.AirportConditionsText;
            this.SelectedPreset.Notams = this.NotamText;

            if (this.SandboxMetar != null)
            {
                var decodedMetar = this._metarDecoder.ParseNotStrict(this.SandboxMetar);
                this.AtisBuilderResponse = await this._atisBuilder.BuildAtis(
                    this.SelectedStation,
                    this.SelectedPreset,
                    randomLetter,
                    decodedMetar,
                    this._cancellationToken.Token,
                    true);
                this.SandboxTextAtis = this.AtisBuilderResponse.TextAtis?.ToUpperInvariant();
                this.SandboxSpokenTextAtis = this.AtisBuilderResponse.SpokenText?.ToUpperInvariant();
            }
        }
        catch (Exception)
        {
            this.SandboxTextAtis = "";
            this.SandboxSpokenTextAtis = "";
            throw;
        }
    }

    private void HandleSaveNotamsText()
    {
        if (this.SelectedPreset == null)
        {
            return;
        }

        this.SelectedPreset.Notams = this.NotamText;

        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedNotams = false;
    }

    private async Task HandleOpenStaticNotamsDialog()
    {
        if (this.DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (lifetime.MainWindow == null)
        {
            return;
        }

        if (this.SelectedStation == null)
        {
            return;
        }

        var dlg = this._windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(this.SelectedStation.NotamDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this.SelectedStation.NotamsBeforeFreeText = val;
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.SelectedStation.NotamDefinitions.Clear();
                    this.SelectedStation.NotamDefinitions.AddRange(changes);
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this.SelectedStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this.SelectedStation.NotamDefinitions.Add(item);
                }

                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
                }
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);
    }

    private void HandleSaveAirportConditionsText()
    {
        if (this.SelectedPreset == null)
        {
            return;
        }

        this.SelectedPreset.AirportConditions = this.AirportConditionsText;
        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedAirportConditions = false;
    }

    private async Task HandleOpenStaticAirportConditionsDialog()
    {
        if (this.DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (lifetime.MainWindow == null)
        {
            return;
        }

        if (this.SelectedStation == null)
        {
            return;
        }

        var dlg = this._windowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions =
                new ObservableCollection<StaticDefinition>(this.SelectedStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this.SelectedStation.AirportConditionsBeforeFreeText = val;
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.SelectedStation.AirportConditionDefinitions.Clear();
                    this.SelectedStation.AirportConditionDefinitions.AddRange(changes);
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this.SelectedStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this.SelectedStation.AirportConditionDefinitions.Add(item);
                }

                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
                }
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);
    }

    private void HandleSelectedPresetChanged()
    {
        if (this.SelectedPreset == null)
        {
            return;
        }

        this.AirportConditionsText = this.SelectedPreset.AirportConditions?.ToUpperInvariant() ?? "";
        this.NotamText = this.SelectedPreset.Notams?.ToUpperInvariant() ?? "";
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (this.SelectedStation == null || string.IsNullOrEmpty(this.SelectedStation.Identifier))
        {
            return;
        }

        var metar = await this._metarRepository.GetMetar(
            this.SelectedStation.Identifier,
            false,
            false);
        this.SandboxMetar = metar?.RawMetar;
    }

    #region Reactive Properties

    private ObservableCollection<AtisPreset>? _presets;

    public ObservableCollection<AtisPreset>? Presets
    {
        get => this._presets;
        set => this.RaiseAndSetIfChanged(ref this._presets, value);
    }

    private AtisPreset? _selectedPreset;

    public AtisPreset? SelectedPreset
    {
        get => this._selectedPreset;
        set => this.RaiseAndSetIfChanged(ref this._selectedPreset, value);
    }

    private AtisStation? _selectedStation;

    private AtisStation? SelectedStation
    {
        get => this._selectedStation;
        set => this.RaiseAndSetIfChanged(ref this._selectedStation, value);
    }

    private string? _sandboxMetar;

    public string? SandboxMetar
    {
        get => this._sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref this._sandboxMetar, value);
    }

    private bool _hasUnsavedAirportConditions;

    public bool HasUnsavedAirportConditions
    {
        get => this._hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref this._hasUnsavedAirportConditions, value);
    }

    private bool _hasUnsavedNotams;

    public bool HasUnsavedNotams
    {
        get => this._hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref this._hasUnsavedNotams, value);
    }

    private string? AirportConditionsText
    {
        get => this._airportConditionsTextDocument.Text ?? "";
        set => this.AirportConditionsTextDocument = new TextDocument(value);
    }

    private TextDocument _airportConditionsTextDocument = new();

    public TextDocument AirportConditionsTextDocument
    {
        get => this._airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this._airportConditionsTextDocument, value);
    }

    private string? NotamText
    {
        get => this._notamsTextDocument.Text ?? "";
        set => this.NotamsTextDocument = new TextDocument(value);
    }

    private TextDocument _notamsTextDocument = new();

    public TextDocument NotamsTextDocument
    {
        get => this._notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this._notamsTextDocument, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];

    public List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
    }

    private string? _sandboxTextAtis;

    public string? SandboxTextAtis
    {
        get => this._sandboxTextAtis;
        set => this.RaiseAndSetIfChanged(ref this._sandboxTextAtis, value);
    }

    private string? _sandboxSpokenTextAtis;

    public string? SandboxSpokenTextAtis
    {
        get => this._sandboxSpokenTextAtis;
        set => this.RaiseAndSetIfChanged(ref this._sandboxSpokenTextAtis, value);
    }

    private bool _isSandboxPlaybackActive;

    public bool IsSandboxPlaybackActive
    {
        get => this._isSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref this._isSandboxPlaybackActive, value);
    }

    private AtisBuilderResponse? _atisBuilderResponse;

    private AtisBuilderResponse? AtisBuilderResponse
    {
        get => this._atisBuilderResponse;
        set => this.RaiseAndSetIfChanged(ref this._atisBuilderResponse, value);
    }

    #endregion
}