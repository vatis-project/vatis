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
    private readonly IProfileRepository mProfileRepository;
    private readonly ISessionManager mSessionManager;
    private readonly IWindowFactory mWindowFactory;
    private readonly IAtisBuilder mAtisBuilder;
    private readonly IMetarRepository mMetarRepository;
    private CancellationTokenSource mCancellationToken;
    private readonly Random mRandom = new();
    private readonly MetarDecoder mMetarDecoder = new();
    
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

    private ObservableCollection<AtisPreset>? mPresets;

    public ObservableCollection<AtisPreset>? Presets
    {
        get => mPresets;
        set => this.RaiseAndSetIfChanged(ref mPresets, value);
    }

    private AtisPreset? mSelectedPreset;

    public AtisPreset? SelectedPreset
    {
        get => mSelectedPreset;
        set => this.RaiseAndSetIfChanged(ref mSelectedPreset, value);
    }

    private AtisStation? mSelectedStation;

    private AtisStation? SelectedStation
    {
        get => mSelectedStation;
        set => this.RaiseAndSetIfChanged(ref mSelectedStation, value);
    }

    private string? mSandboxMetar;

    public string? SandboxMetar
    {
        get => mSandboxMetar;
        set => this.RaiseAndSetIfChanged(ref mSandboxMetar, value);
    }

    private bool mHasUnsavedAirportConditions;

    public bool HasUnsavedAirportConditions
    {
        get => mHasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref mHasUnsavedAirportConditions, value);
    }

    private bool mHasUnsavedNotams;

    public bool HasUnsavedNotams
    {
        get => mHasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref mHasUnsavedNotams, value);
    }

    private string? AirportConditionsText
    {
        get => mAirportConditionsTextDocument.Text ?? "";
        set => AirportConditionsTextDocument = new TextDocument(value);
    }

    private TextDocument mAirportConditionsTextDocument = new();

    public TextDocument AirportConditionsTextDocument
    {
        get => mAirportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref mAirportConditionsTextDocument, value);
    }

    private string? NotamText
    {
        get => mNotamsTextDocument.Text ?? "";
        set => NotamsTextDocument = new TextDocument(value);
    }

    private TextDocument mNotamsTextDocument = new();

    public TextDocument NotamsTextDocument
    {
        get => mNotamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref mNotamsTextDocument, value);
    }

    private List<ICompletionData> mContractionCompletionData = [];

    public List<ICompletionData> ContractionCompletionData
    {
        get => mContractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref mContractionCompletionData, value);
    }

    private string? mSandboxTextAtis;

    public string? SandboxTextAtis
    {
        get => mSandboxTextAtis;
        set => this.RaiseAndSetIfChanged(ref mSandboxTextAtis, value);
    }

    private string? mSandboxSpokenTextAtis;

    public string? SandboxSpokenTextAtis
    {
        get => mSandboxSpokenTextAtis;
        set => this.RaiseAndSetIfChanged(ref mSandboxSpokenTextAtis, value);
    }

    private bool mIsSandboxPlaybackActive;

    public bool IsSandboxPlaybackActive
    {
        get => mIsSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref mIsSandboxPlaybackActive, value);
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
        mWindowFactory = windowFactory;
        mAtisBuilder = atisBuilder;
        mMetarRepository = metarRepository;
        mProfileRepository = profileRepository;
        mSessionManager = sessionManager;
        mCancellationToken = new CancellationTokenSource();

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

        await mCancellationToken.CancelAsync();
        mCancellationToken.Dispose();
        mCancellationToken = new CancellationTokenSource();

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

            await mCancellationToken.CancelAsync();
            mCancellationToken.Dispose();
            mCancellationToken = new CancellationTokenSource();

            SandboxTextAtis = "Loading...";
            SandboxSpokenTextAtis = "Loading...";

            var randomLetter =
                (char)mRandom.Next(SelectedStation.CodeRange.Low + SelectedStation.CodeRange.High + 1);

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

        if (mSessionManager.CurrentProfile != null)
            mProfileRepository.Save(mSessionManager.CurrentProfile);

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

        var dlg = mWindowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(SelectedStation.NotamDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                SelectedStation.NotamsBeforeFreeText = val;
                if (mSessionManager.CurrentProfile != null)
                    mProfileRepository.Save(mSessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    SelectedStation.NotamDefinitions.Clear();
                    SelectedStation.NotamDefinitions.AddRange(changes);
                    if (mSessionManager.CurrentProfile != null)
                        mProfileRepository.Save(mSessionManager.CurrentProfile);
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

                if (mSessionManager.CurrentProfile != null)
                    mProfileRepository.Save(mSessionManager.CurrentProfile);
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedPreset == null)
            return;

        SelectedPreset.AirportConditions = AirportConditionsText;
        if (mSessionManager.CurrentProfile != null)
            mProfileRepository.Save(mSessionManager.CurrentProfile);

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

        var dlg = mWindowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions =
                new ObservableCollection<StaticDefinition>(SelectedStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                SelectedStation.AirportConditionsBeforeFreeText = val;
                if (mSessionManager.CurrentProfile != null)
                    mProfileRepository.Save(mSessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    SelectedStation.AirportConditionDefinitions.Clear();
                    SelectedStation.AirportConditionDefinitions.AddRange(changes);
                    if (mSessionManager.CurrentProfile != null)
                        mProfileRepository.Save(mSessionManager.CurrentProfile);
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

                if (mSessionManager.CurrentProfile != null)
                    mProfileRepository.Save(mSessionManager.CurrentProfile);
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

        var metar = await mMetarRepository.GetMetar(SelectedStation.Identifier, monitor: false,
            triggerMessageBus: false);
        SandboxMetar = metar?.RawMetar;
    }
}