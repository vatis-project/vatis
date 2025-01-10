// <copyright file="SandboxViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the view model for the sandbox environment.
/// </summary>
public class SandboxViewModel : ReactiveViewModelBase
{
    private readonly IAtisBuilder atisBuilder;
    private readonly MetarDecoder metarDecoder = new();
    private readonly IMetarRepository metarRepository;
    private readonly IProfileRepository profileRepository;
    private readonly Random random = new();
    private readonly ISessionManager sessionManager;
    private readonly IWindowFactory windowFactory;
    private CancellationTokenSource cancellationToken;
    private ObservableCollection<AtisPreset>? presets;
    private AtisPreset? selectedPreset;
    private AtisStation? selectedStation;
    private string? sandboxMetar;
    private bool hasUnsavedAirportConditions;
    private bool hasUnsavedNotams;
    private TextDocument airportConditionsTextDocument = new();
    private TextDocument notamsTextDocument = new();
    private List<ICompletionData> contractionCompletionData = new();
    private string? sandboxTextAtis;
    private string? sandboxSpokenTextAtis;
    private bool isSandboxPlaybackActive;
    private AtisBuilderResponse? atisBuilderResponse;

    /// <summary>
    /// Initializes a new instance of the <see cref="SandboxViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">An instance of <see cref="IWindowFactory"/> used for creating application windows.</param>
    /// <param name="atisBuilder">An instance of <see cref="IAtisBuilder"/> used for building ATIS.</param>
    /// <param name="metarRepository">An instance of <see cref="IMetarRepository"/> used for accessing METAR data.</param>
    /// <param name="profileRepository">An instance of <see cref="IProfileRepository"/> used for managing user profiles.</param>
    /// <param name="sessionManager">An instance of <see cref="ISessionManager"/> used for managing sessions.</param>
    public SandboxViewModel(
        IWindowFactory windowFactory,
        IAtisBuilder atisBuilder,
        IMetarRepository metarRepository,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        this.windowFactory = windowFactory;
        this.atisBuilder = atisBuilder;
        this.metarRepository = metarRepository;
        this.profileRepository = profileRepository;
        this.sessionManager = sessionManager;
        this.cancellationToken = new CancellationTokenSource();

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
        get => this.presets;
        set => this.RaiseAndSetIfChanged(ref this.presets, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedPreset
    {
        get => this.selectedPreset;
        set => this.RaiseAndSetIfChanged(ref this.selectedPreset, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => this.selectedStation;
        set => this.RaiseAndSetIfChanged(ref this.selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the sandbox METAR.
    /// </summary>
    public string? SandboxMetar
    {
        get => this.sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref this.sandboxMetar, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved airport conditions.
    /// </summary>
    public bool HasUnsavedAirportConditions
    {
        get => this.hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedAirportConditions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved NOTAMs.
    /// </summary>
    public bool HasUnsavedNotams
    {
        get => this.hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedNotams, value);
    }

    /// <summary>
    /// Gets or sets the airport conditions text.
    /// </summary>
    public string? AirportConditionsText
    {
        get => this.airportConditionsTextDocument.Text ?? string.Empty;
        set => this.AirportConditionsTextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the airport conditions text document.
    /// </summary>
    public TextDocument AirportConditionsTextDocument
    {
        get => this.airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this.airportConditionsTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the NOTAMs text.
    /// </summary>
    public string? NotamText
    {
        get => this.notamsTextDocument.Text ?? string.Empty;
        set => this.NotamsTextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the NOTAMs text document.
    /// </summary>
    public TextDocument NotamsTextDocument
    {
        get => this.notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this.notamsTextDocument, value);
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
    /// Gets or sets the sandbox text ATIS.
    /// </summary>
    public string? SandboxTextAtis
    {
        get => this.sandboxTextAtis;
        set => this.RaiseAndSetIfChanged(ref this.sandboxTextAtis, value);
    }

    /// <summary>
    /// Gets or sets the sandbox spoken text ATIS.
    /// </summary>
    public string? SandboxSpokenTextAtis
    {
        get => this.sandboxSpokenTextAtis;
        set => this.RaiseAndSetIfChanged(ref this.sandboxSpokenTextAtis, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sandbox playback is active.
    /// </summary>
    public bool IsSandboxPlaybackActive
    {
        get => this.isSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref this.isSandboxPlaybackActive, value);
    }

    /// <summary>
    /// Gets or sets the ATIS builder response.
    /// </summary>
    public AtisBuilderResponse? AtisBuilderResponse
    {
        get => this.atisBuilderResponse;
        set => this.RaiseAndSetIfChanged(ref this.atisBuilderResponse, value);
    }

    /// <summary>
    /// Applies sandbox configuration by verifying the state of unsaved data and stopping sandbox playback if required.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if the configuration is applied successfully; <c>false</c> if there are unsaved airport conditions or NOTAMs.
    /// </returns>
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

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedPreset = null;
        this.SelectedStation = station;
        this.Presets = new ObservableCollection<AtisPreset>(station.Presets);
        this.SandboxMetar = string.Empty;
        this.HasUnsavedAirportConditions = false;
        this.HasUnsavedNotams = false;
        this.AirportConditionsText = string.Empty;
        this.NotamText = string.Empty;
        this.SandboxTextAtis = string.Empty;
        this.SandboxSpokenTextAtis = string.Empty;
        this.IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();
    }

    private async Task HandlePlaySandboxAtis(CancellationToken token)
    {
        if (this.SelectedStation == null || this.SelectedPreset == null || this.AtisBuilderResponse == null)
        {
            return;
        }

        await this.cancellationToken.CancelAsync();
        this.cancellationToken.Dispose();
        this.cancellationToken = new CancellationTokenSource();

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

            await this.cancellationToken.CancelAsync();
            this.cancellationToken.Dispose();
            this.cancellationToken = new CancellationTokenSource();

            this.SandboxTextAtis = "Loading...";
            this.SandboxSpokenTextAtis = "Loading...";

            var randomLetter =
                (char)this.random.Next(this.SelectedStation.CodeRange.Low + this.SelectedStation.CodeRange.High + 1);

            if (randomLetter is < 'A' or > 'Z')
            {
                randomLetter = 'A';
            }

            this.SelectedPreset.AirportConditions = this.AirportConditionsText;
            this.SelectedPreset.Notams = this.NotamText;

            if (this.SandboxMetar != null)
            {
                var decodedMetar = this.metarDecoder.ParseNotStrict(this.SandboxMetar);
                this.AtisBuilderResponse = await this.atisBuilder.BuildAtis(
                    this.SelectedStation,
                    this.SelectedPreset,
                    randomLetter,
                    decodedMetar,
                    this.cancellationToken.Token,
                    true);
                this.SandboxTextAtis = this.AtisBuilderResponse.TextAtis?.ToUpperInvariant();
                this.SandboxSpokenTextAtis = this.AtisBuilderResponse.SpokenText?.ToUpperInvariant();
            }
        }
        catch (Exception)
        {
            this.SandboxTextAtis = string.Empty;
            this.SandboxSpokenTextAtis = string.Empty;
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

        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
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

        var dlg = this.windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(this.SelectedStation.NotamDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this.SelectedStation.NotamsBeforeFreeText = val;
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.SelectedStation.NotamDefinitions.Clear();
                    this.SelectedStation.NotamDefinitions.AddRange(changes);
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
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

                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
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
        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
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

        var dlg = this.windowFactory.CreateStaticAirportConditionsDialog();
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
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.SelectedStation.AirportConditionDefinitions.Clear();
                    this.SelectedStation.AirportConditionDefinitions.AddRange(changes);
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
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

                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
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

        this.AirportConditionsText = this.SelectedPreset.AirportConditions?.ToUpperInvariant() ?? string.Empty;
        this.NotamText = this.SelectedPreset.Notams?.ToUpperInvariant() ?? string.Empty;
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (this.SelectedStation == null || string.IsNullOrEmpty(this.SelectedStation.Identifier))
        {
            return;
        }

        var metar = await this.metarRepository.GetMetar(
            this.SelectedStation.Identifier,
            false,
            false);
        this.SandboxMetar = metar?.RawMetar;
    }
}
