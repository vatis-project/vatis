// <copyright file="AtisStationViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Voice.Utils;
using Vatsim.Vatis.Weather.Decoder.Entity;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a ViewModel for managing ATIS station information and operations.
/// </summary>
public class AtisStationViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig appConfig;
    private readonly IAtisBuilder atisBuilder;
    private readonly IAtisHubConnection atisHubConnection;
    private readonly AtisStation atisStation;
    private readonly Airport atisStationAirport;
    private readonly INetworkConnection? networkConnection;
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private readonly IVoiceServerConnection? voiceServerConnection;
    private readonly IWebsocketService websocketService;
    private readonly IWindowFactory windowFactory;
    private int airportConditionsFreeTextOffset;
    private CancellationTokenSource cancellationToken;
    private DecodedMetar? decodedMetar;
    private bool isPublishAtisTriggeredInitially;
    private int notamFreeTextOffset;
    private AtisPreset? previousAtisPreset;
    private IDisposable? publishAtisTimer;
    private string? id;
    private string? identifier;
    private string? tabText;
    private char atisLetter;
    private bool isAtisLetterInputMode;
    private string? metarString;
    private string? wind;
    private string? altimeter;
    private bool isNewAtis;
    private string atisTypeLabel = string.Empty;
    private bool isCombinedAtis;
    private ObservableCollection<AtisPreset> atisPresetList = [];
    private AtisPreset? selectedAtisPreset;
    private string? errorMessage;
    private TextDocument? airportConditionsTextDocument = new();
    private TextDocument? notamsTextDocument = new();
    private bool useTexToSpeech;
    private NetworkConnectionStatus networkConnectionStatus = NetworkConnectionStatus.Disconnected;
    private List<ICompletionData> contractionCompletionData = [];
    private bool hasUnsavedAirportConditions;
    private bool hasUnsavedNotams;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisStationViewModel"/> class.
    /// </summary>
    /// <param name="station">
    /// The ATIS station instance associated with this view model.
    /// </param>
    /// <param name="connectionFactory">
    /// The network connection factory used to manage network connections.
    /// </param>
    /// <param name="appConfig">
    /// The application configuration used for accessing app settings.
    /// </param>
    /// <param name="voiceServerConnection">
    /// The voice server connection instance for handling voice communication.
    /// </param>
    /// <param name="atisBuilder">
    /// The ATIS builder used to construct ATIS messages.
    /// </param>
    /// <param name="windowFactory">
    /// The window factory used for creating application windows.
    /// </param>
    /// <param name="navDataRepository">
    /// The navigation data repository providing access to navigation data.
    /// </param>
    /// <param name="hubConnection">
    /// The ATIS hub connection for interacting with the central hub.
    /// </param>
    /// <param name="sessionManager">
    /// The session manager handling active user sessions.
    /// </param>
    /// <param name="profileRepository">
    /// The profile repository for accessing user profiles.
    /// </param>
    /// <param name="websocketService">
    /// The websocket service for handling websocket communications.
    /// </param>
    public AtisStationViewModel(
        AtisStation station,
        INetworkConnectionFactory connectionFactory,
        IAppConfig appConfig,
        IVoiceServerConnection voiceServerConnection,
        IAtisBuilder atisBuilder,
        IWindowFactory windowFactory,
        INavDataRepository navDataRepository,
        IAtisHubConnection hubConnection,
        ISessionManager sessionManager,
        IProfileRepository profileRepository,
        IWebsocketService websocketService)
    {
        this.Id = station.Id;
        this.Identifier = station.Identifier;
        this.atisStation = station;
        this.appConfig = appConfig;
        this.atisBuilder = atisBuilder;
        this.windowFactory = windowFactory;
        this.websocketService = websocketService;
        this.atisHubConnection = hubConnection;
        this.sessionManager = sessionManager;
        this.profileRepository = profileRepository;
        this.cancellationToken = new CancellationTokenSource();
        this.atisStationAirport = navDataRepository.GetAirport(station.Identifier) ??
                                  throw new ApplicationException($"{station.Identifier} not found in airport navdata.");

        this.atisLetter = this.atisStation.CodeRange.Low;

        this.ReadOnlyAirportConditions = new TextSegmentCollection<TextSegment>(this.AirportConditionsTextDocument);
        this.ReadOnlyNotams = new TextSegmentCollection<TextSegment>(this.NotamsTextDocument);

        switch (station.AtisType)
        {
            case AtisType.Arrival:
                this.TabText = $"{this.Identifier}/A";
                this.AtisTypeLabel = "ARR";
                break;
            case AtisType.Departure:
                this.TabText = $"{this.Identifier}/D";
                this.AtisTypeLabel = "DEP";
                break;
            case AtisType.Combined:
                this.TabText = this.Identifier;
                this.AtisTypeLabel = string.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        this.IsCombinedAtis = station.AtisType == AtisType.Combined;
        this.AtisPresetList = new ObservableCollection<AtisPreset>(station.Presets.OrderBy(x => x.Ordinal));

        this.OpenStaticAirportConditionsDialogCommand =
            ReactiveCommand.CreateFromTask(this.HandleOpenAirportConditionsDialog);
        this.OpenStaticNotamsDialogCommand = ReactiveCommand.CreateFromTask(this.HandleOpenStaticNotamsDialog);

        this.SaveAirportConditionsText = ReactiveCommand.Create(this.HandleSaveAirportConditionsText);
        this.SaveNotamsText = ReactiveCommand.Create(this.HandleSaveNotamsText);
        this.SelectedPresetChangedCommand =
            ReactiveCommand.CreateFromTask<AtisPreset>(this.HandleSelectedAtisPresetChanged);
        this.AcknowledgeAtisUpdateCommand = ReactiveCommand.Create(this.HandleAcknowledgeAtisUpdate);
        this.DecrementAtisLetterCommand = ReactiveCommand.Create(this.DecrementAtisLetter);
        this.AcknowledgeOrIncrementAtisLetterCommand = ReactiveCommand.Create(this.AcknowledgeOrIncrementAtisLetter);
        this.NetworkConnectCommand = ReactiveCommand.Create(
            this.HandleNetworkConnect,
            this.WhenAnyValue(
                x => x.SelectedAtisPreset,
                x => x.NetworkConnectionStatus,
                (atisPreset, networkStatus) =>
                    atisPreset != null && networkStatus != NetworkConnectionStatus.Connecting));
        this.VoiceRecordAtisCommand = ReactiveCommand.Create(
            this.HandleVoiceRecordAtisCommand,
            this.WhenAnyValue(
                x => x.Metar,
                x => x.UseTexToSpeech,
                x => x.NetworkConnectionStatus,
                (metar, voiceRecord, networkStatus) => !string.IsNullOrEmpty(metar) && voiceRecord &&
                                                       networkStatus == NetworkConnectionStatus.Connected));

        this.websocketService.GetAtisReceived += this.OnGetAtisReceived;
        this.websocketService.AcknowledgeAtisUpdateReceived += this.OnAcknowledgeAtisUpdateReceived;

        this.LoadContractionData();

        this.networkConnection = connectionFactory.CreateConnection(this.atisStation);
        this.networkConnection.NetworkConnectionFailed += this.OnNetworkConnectionFailed;
        this.networkConnection.NetworkErrorReceived += this.OnNetworkErrorReceived;
        this.networkConnection.NetworkConnected += this.OnNetworkConnected;
        this.networkConnection.NetworkDisconnected += this.OnNetworkDisconnected;
        this.networkConnection.ChangeServerReceived += this.OnChangeServerReceived;
        this.networkConnection.MetarResponseReceived += this.OnMetarResponseReceived;
        this.networkConnection.KillRequestReceived += this.OnKillRequestedReceived;
        this.voiceServerConnection = voiceServerConnection;

        this.UseTexToSpeech = !this.atisStation.AtisVoice.UseTextToSpeech;
        MessageBus.Current.Listen<AtisVoiceTypeChanged>().Subscribe(
            evt =>
            {
                if (evt.Id == this.atisStation.Id)
                {
                    this.UseTexToSpeech = !evt.UseTextToSpeech;
                }
            });
        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(
            evt =>
            {
                if (evt.Id == this.atisStation.Id)
                {
                    this.AtisPresetList =
                        new ObservableCollection<AtisPreset>(this.atisStation.Presets.OrderBy(x => x.Ordinal));
                }
            });
        MessageBus.Current.Listen<ContractionsUpdated>().Subscribe(
            evt =>
            {
                if (evt.StationId == this.atisStation.Id)
                {
                    this.LoadContractionData();
                }
            });
        MessageBus.Current.Listen<AtisHubAtisReceived>().Subscribe(
            sync =>
            {
                if (sync.Dto.StationId == station.Identifier &&
                    sync.Dto.AtisType == station.AtisType &&
                    this.NetworkConnectionStatus != NetworkConnectionStatus.Connected)
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            this.AtisLetter = sync.Dto.AtisLetter;
                            this.Wind = sync.Dto.Wind;
                            this.Altimeter = sync.Dto.Altimeter;
                            this.Metar = sync.Dto.Metar;
                            this.NetworkConnectionStatus = NetworkConnectionStatus.Observer;
                        });
                }
            });
        MessageBus.Current.Listen<AtisHubExpiredAtisReceived>().Subscribe(
            sync =>
            {
                if (sync.Dto.StationId == this.atisStation.Identifier &&
                    sync.Dto.AtisType == this.atisStation.AtisType &&
                    this.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            this.AtisLetter = this.atisStation.CodeRange.Low;
                            this.Wind = null;
                            this.Altimeter = null;
                            this.Metar = null;
                            this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                        });
                }
            });
        MessageBus.Current.Listen<HubConnected>().Subscribe(
            _ =>
            {
                this.atisHubConnection.SubscribeToAtis(
                    new SubscribeDto(this.atisStation.Identifier, this.atisStation.AtisType));
            });

        this.WhenAnyValue(x => x.IsNewAtis).Subscribe(this.HandleIsNewAtisChanged);
        this.WhenAnyValue(x => x.AtisLetter).Subscribe(this.HandleAtisLetterChanged);
        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1).Subscribe(this.HandleNetworkStatusChanged);
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
    /// Gets the command to decrement the ATIS letter.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DecrementAtisLetterCommand { get; }

    /// <summary>
    /// Gets the command to acknowledge or increment the ATIS letter.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AcknowledgeOrIncrementAtisLetterCommand { get; }

    /// <summary>
    /// Gets the command to acknowledge an ATIS update.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AcknowledgeAtisUpdateCommand { get; }

    /// <summary>
    /// Gets the command for initiating a network connection.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NetworkConnectCommand { get; }

    /// <summary>
    /// Gets the command used to initiate voice recording for ATIS.
    /// </summary>
    public ReactiveCommand<Unit, Unit> VoiceRecordAtisCommand { get; }

    /// <summary>
    /// Gets the command to open the static airport conditions dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenStaticAirportConditionsDialogCommand { get; }

    /// <summary>
    /// Gets the command for opening the static NOTAMs dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenStaticNotamsDialogCommand { get; }

    /// <summary>
    /// Gets the command executed when the selected ATIS preset changes.
    /// </summary>
    public ReactiveCommand<AtisPreset, Unit> SelectedPresetChangedCommand { get; }

    /// <summary>
    /// Gets the command to save the airport condition free-form text.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveAirportConditionsText { get; }

    /// <summary>
    /// Gets the command to save NOTAMs free-form text.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveNotamsText { get; }

    /// <summary>
    /// Gets the unique identifier for the ATIS station.
    /// </summary>
    public string? Id
    {
        get => this.id;
        private set => this.RaiseAndSetIfChanged(ref this.id, value);
    }

    /// <summary>
    /// Gets or sets the identifier associated with the ATIS station.
    /// </summary>
    public string? Identifier
    {
        get => this.identifier;
        set => this.RaiseAndSetIfChanged(ref this.identifier, value);
    }

    /// <summary>
    /// Gets or sets the text displayed on the tab for the ATIS station.
    /// </summary>
    public string? TabText
    {
        get => this.tabText;
        set => this.RaiseAndSetIfChanged(ref this.tabText, value);
    }

    /// <summary>
    /// Gets or sets the ATIS letter associated with the station.
    /// </summary>
    public char AtisLetter
    {
        get => this.atisLetter;
        set => this.RaiseAndSetIfChanged(ref this.atisLetter, value);
    }

    /// <summary>
    /// Gets the range of valid ATIS code letters associated with the ATIS station.
    /// </summary>
    public CodeRangeMeta CodeRange => this.atisStation.CodeRange;

    /// <summary>
    /// Gets or sets a value indicating whether the ATIS letter input mode is active.
    /// </summary>
    public bool IsAtisLetterInputMode
    {
        get => this.isAtisLetterInputMode;
        set => this.RaiseAndSetIfChanged(ref this.isAtisLetterInputMode, value);
    }

    /// <summary>
    /// Gets or sets the METAR string for the ATIS station.
    /// </summary>
    public string? Metar
    {
        get => this.metarString;
        set => this.RaiseAndSetIfChanged(ref this.metarString, value);
    }

    /// <summary>
    /// Gets or sets the wind information associated with the ATIS station.
    /// </summary>
    public string? Wind
    {
        get => this.wind;
        set => this.RaiseAndSetIfChanged(ref this.wind, value);
    }

    /// <summary>
    /// Gets or sets the altimeter value as a string representation.
    /// </summary>
    public string? Altimeter
    {
        get => this.altimeter;
        set => this.RaiseAndSetIfChanged(ref this.altimeter, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ATIS is new.
    /// </summary>
    public bool IsNewAtis
    {
        get => this.isNewAtis;
        set => this.RaiseAndSetIfChanged(ref this.isNewAtis, value);
    }

    /// <summary>
    /// Gets or sets the ATIS type label.
    /// </summary>
    public string AtisTypeLabel
    {
        get => this.atisTypeLabel;
        set => this.RaiseAndSetIfChanged(ref this.atisTypeLabel, value);
    }

    /// <summary>
    /// Gets a value indicating whether the ATIS station type is "Combined".
    /// </summary>
    public bool IsCombinedAtis
    {
        get => this.isCombinedAtis;
        private set => this.RaiseAndSetIfChanged(ref this.isCombinedAtis, value);
    }

    /// <summary>
    /// Gets or sets the collection of ATIS presets.
    /// </summary>
    public ObservableCollection<AtisPreset> AtisPresetList
    {
        get => this.atisPresetList;
        set => this.RaiseAndSetIfChanged(ref this.atisPresetList, value);
    }

    /// <summary>
    /// Gets the currently selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedAtisPreset
    {
        get => this.selectedAtisPreset;
        private set => this.RaiseAndSetIfChanged(ref this.selectedAtisPreset, value);
    }

    /// <summary>
    /// Gets or sets the error message associated with the current operation or state.
    /// </summary>
    public string? ErrorMessage
    {
        get => this.errorMessage;
        set => this.RaiseAndSetIfChanged(ref this.errorMessage, value);
    }

    /// <summary>
    /// Gets the free text representation of the airport conditions.
    /// </summary>
    public string? AirportConditionsFreeText => this.AirportConditionsTextDocument?.Text;

    /// <summary>
    /// Gets or sets the text document containing airport conditions.
    /// </summary>
    public TextDocument? AirportConditionsTextDocument
    {
        get => this.airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this.airportConditionsTextDocument, value);
    }

    /// <summary>
    /// Gets the free-text representation of the NOTAMs from the text document.
    /// </summary>
    public string? NotamsFreeText => this.notamsTextDocument?.Text;

    /// <summary>
    /// Gets or sets the NOTAMs text document for editing operations.
    /// </summary>
    public TextDocument? NotamsTextDocument
    {
        get => this.notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this.notamsTextDocument, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether text-to-speech functionality is enabled.
    /// </summary>
    public bool UseTexToSpeech
    {
        get => this.useTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref this.useTexToSpeech, value);
    }

    /// <summary>
    /// Gets or sets the network connection status of the ATIS station.
    /// </summary>
    public NetworkConnectionStatus NetworkConnectionStatus
    {
        get => this.networkConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref this.networkConnectionStatus, value);
    }

    /// <summary>
    /// Gets or sets the collection of contraction completion data utilized for auto-completion.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => this.contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this.contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes to the airport conditions.
    /// </summary>
    public bool HasUnsavedAirportConditions
    {
        get => this.hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedAirportConditions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes to the NOTAMs.
    /// </summary>
    public bool HasUnsavedNotams
    {
        get => this.hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedNotams, value);
    }

    /// <summary>
    /// Disconnects the current network connection and updates the network connection status
    /// to <see cref="NetworkConnectionStatus.Disconnected"/>.
    /// </summary>
    public void Disconnect()
    {
        this.networkConnection?.Disconnect();
        this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this.websocketService.GetAtisReceived -= this.OnGetAtisReceived;
        this.websocketService.AcknowledgeAtisUpdateReceived -= this.OnAcknowledgeAtisUpdateReceived;

        if (this.networkConnection != null)
        {
            this.networkConnection.NetworkConnectionFailed -= this.OnNetworkConnectionFailed;
            this.networkConnection.NetworkErrorReceived -= this.OnNetworkErrorReceived;
            this.networkConnection.NetworkConnected -= this.OnNetworkConnected;
            this.networkConnection.NetworkDisconnected -= this.OnNetworkDisconnected;
            this.networkConnection.ChangeServerReceived -= this.OnChangeServerReceived;
            this.networkConnection.MetarResponseReceived -= this.OnMetarResponseReceived;
            this.networkConnection.KillRequestReceived -= this.OnKillRequestedReceived;
        }

        this.DecrementAtisLetterCommand.Dispose();
        this.AcknowledgeOrIncrementAtisLetterCommand.Dispose();
        this.AcknowledgeAtisUpdateCommand.Dispose();
        this.NetworkConnectCommand.Dispose();
        this.VoiceRecordAtisCommand.Dispose();
        this.OpenStaticAirportConditionsDialogCommand.Dispose();
        this.OpenStaticNotamsDialogCommand.Dispose();
        this.SelectedPresetChangedCommand.Dispose();
        this.SaveAirportConditionsText.Dispose();
        this.SaveNotamsText.Dispose();
    }

    private void HandleSaveNotamsText()
    {
        if (this.SelectedAtisPreset == null)
        {
            return;
        }

        this.SelectedAtisPreset.Notams = this.NotamsFreeText?[this.notamFreeTextOffset..];
        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
        }

        this.HasUnsavedNotams = false;
    }

    private void HandleSaveAirportConditionsText()
    {
        if (this.SelectedAtisPreset == null)
        {
            return;
        }

        this.SelectedAtisPreset.AirportConditions =
            this.AirportConditionsFreeText?[this.airportConditionsFreeTextOffset..];
        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
        }

        this.HasUnsavedAirportConditions = false;
    }

    private void LoadContractionData()
    {
        this.ContractionCompletionData.Clear();

        foreach (var contraction in this.atisStation.Contractions.ToList())
        {
            if (contraction is { VariableName: not null, Voice: not null })
            {
                this.ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }
    }

    private async Task HandleOpenStaticNotamsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (lifetime.MainWindow == null)
        {
            return;
        }

        var dlg = this.windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(this.atisStation.NotamDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this.atisStation.NotamsBeforeFreeText = val;
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.atisStation.NotamDefinitions.Clear();
                    this.atisStation.NotamDefinitions.AddRange(changes);
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this.atisStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this.atisStation.NotamDefinitions.Add(item);
                }

                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
                }
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        // Update the free-form text area after the dialog is closed
        this.PopulateNotams();
    }

    private async Task HandleOpenAirportConditionsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (lifetime.MainWindow == null)
        {
            return;
        }

        var dlg = this.windowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions =
                new ObservableCollection<StaticDefinition>(this.atisStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this.atisStation.AirportConditionsBeforeFreeText = val;
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this.atisStation.AirportConditionDefinitions.Clear();
                    this.atisStation.AirportConditionDefinitions.AddRange(changes);
                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this.atisStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this.atisStation.AirportConditionDefinitions.Add(item);
                }

                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
                }
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        // Update the free-form text area after the dialog is closed
        this.PopulateAirportConditions();
    }

    private void OnKillRequestedReceived(object? sender, KillRequestReceived e)
    {
        NativeAudio.EmitSound(SoundType.Error);

        Dispatcher.UIThread.Post(
            () =>
            {
                this.Wind = null;
                this.Altimeter = null;
                this.Metar = null;
                this.ErrorMessage = string.IsNullOrEmpty(e.Reason)
                    ? "Forcefully disconnected from network."
                    : $"Forcefully disconnected from network: {e.Reason}";
            });
    }

    private async void HandleVoiceRecordAtisCommand()
    {
        try
        {
            if (this.SelectedAtisPreset == null)
            {
                return;
            }

            if (this.networkConnection == null || this.voiceServerConnection == null)
            {
                return;
            }

            if (this.decodedMetar == null)
            {
                return;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                {
                    return;
                }

                var window = this.windowFactory.CreateVoiceRecordAtisDialog();
                if (window.DataContext is VoiceRecordAtisDialogViewModel vm)
                {
                    var atis = await this.atisBuilder.BuildAtis(
                        this.atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        this.decodedMetar,
                        this.cancellationToken.Token);

                    vm.AtisScript = atis.TextAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        await Task.Run(
                            async () =>
                            {
                                this.atisStation.TextAtis = atis.TextAtis;

                                await this.PublishAtisToHub();
                                this.networkConnection.SendSubscriberNotification(this.AtisLetter);
                                await this.atisBuilder.UpdateIds(
                                    this.atisStation,
                                    this.SelectedAtisPreset,
                                    this.AtisLetter,
                                    this.cancellationToken.Token);

                                var dto = AtisBotUtils.AddBotRequest(
                                    vm.AudioBuffer,
                                    this.atisStation.Frequency,
                                    this.atisStationAirport.Latitude,
                                    this.atisStationAirport.Longitude,
                                    100);
                                await this.voiceServerConnection?.AddOrUpdateBot(
                                    this.networkConnection.Callsign,
                                    dto,
                                    this.cancellationToken.Token)!;
                            }).ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    this.ErrorMessage = string.Join(
                                        ",",
                                        t.Exception.InnerExceptions.Select(exception => exception.Message));
                                    this.networkConnection?.Disconnect();
                                    NativeAudio.EmitSound(SoundType.Error);
                                }
                            },
                            this.cancellationToken.Token);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = e.Message;
                });
        }
    }

    private async void HandleNetworkStatusChanged(NetworkConnectionStatus status)
    {
        try
        {
            if (this.voiceServerConnection == null || this.networkConnection == null)
            {
                return;
            }

            await this.PublishAtisToWebsocket();

            switch (status)
            {
                case NetworkConnectionStatus.Connected:
                {
                    try
                    {
                        await this.voiceServerConnection.Connect(
                            this.appConfig.UserId,
                            this.appConfig.PasswordDecrypted);
                        this.sessionManager.CurrentConnectionCount++;
                    }
                    catch (Exception ex)
                    {
                        this.ErrorMessage = ex.Message;
                    }

                    break;
                }

                case NetworkConnectionStatus.Disconnected:
                {
                    try
                    {
                        this.sessionManager.CurrentConnectionCount =
                            Math.Max(this.sessionManager.CurrentConnectionCount - 1, 0);
                        await this.voiceServerConnection.RemoveBot(this.networkConnection.Callsign);
                        this.voiceServerConnection?.Disconnect();
                        this.publishAtisTimer?.Dispose();
                        this.publishAtisTimer = null;
                        this.isPublishAtisTriggeredInitially = false;
                    }
                    catch (Exception ex)
                    {
                        this.ErrorMessage = ex.Message;
                    }

                    break;
                }

                case NetworkConnectionStatus.Connecting:
                case NetworkConnectionStatus.Observer:
                    break;
                default:
                    throw new ApplicationException("Unknown network connection status");
            }
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = e.Message;
                });
        }
    }

    private async void HandleNetworkConnect()
    {
        try
        {
            this.ErrorMessage = null;

            if (this.appConfig.ConfigRequired)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    if (lifetime.MainWindow == null)
                    {
                        return;
                    }

                    if (await MessageBox.ShowDialog(
                            lifetime.MainWindow,
                            "It looks like you haven't set your VATSIM user ID, password, and real name yet. Would you like to set them now?",
                            "Confirm",
                            MessageBoxButton.YesNo,
                            MessageBoxIcon.Information) == MessageBoxResult.Yes)
                    {
                        MessageBus.Current.SendMessage(new OpenGenerateSettingsDialog());
                    }
                }

                return;
            }

            if (this.networkConnection == null)
            {
                return;
            }

            if (!this.networkConnection.IsConnected)
            {
                try
                {
                    if (this.sessionManager.CurrentConnectionCount >= this.sessionManager.MaxConnectionCount)
                    {
                        this.ErrorMessage = "Maximum ATIS connections exceeded.";
                        NativeAudio.EmitSound(SoundType.Error);
                        return;
                    }

                    this.NetworkConnectionStatus = NetworkConnectionStatus.Connecting;
                    await this.networkConnection.Connect();
                }
                catch (Exception e)
                {
                    NativeAudio.EmitSound(SoundType.Error);
                    this.ErrorMessage = e.Message;
                    this.networkConnection?.Disconnect();
                    this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                }
            }
            else
            {
                this.networkConnection?.Disconnect();
                this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
            }
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = e.Message;
                });
        }
    }

    private void OnNetworkConnectionFailed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                this.Metar = null;
                this.Wind = null;
                this.Altimeter = null;
            });
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkErrorReceived(object? sender, NetworkErrorReceived e)
    {
        this.ErrorMessage = e.Error;
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkConnected(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => this.NetworkConnectionStatus = NetworkConnectionStatus.Connected);
    }

    private void OnNetworkDisconnected(object? sender, EventArgs e)
    {
        this.cancellationToken.Cancel();
        this.cancellationToken.Dispose();
        this.cancellationToken = new CancellationTokenSource();

        this.decodedMetar = null;

        Dispatcher.UIThread.Post(
            () =>
            {
                this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                this.Metar = null;
                this.Wind = null;
                this.Altimeter = null;
                this.IsNewAtis = false;
            });
    }

    private void OnChangeServerReceived(object? sender, ClientEventArgs<string> e)
    {
        this.networkConnection?.Disconnect();
        this.networkConnection?.Connect(e.Value);
    }

    private async void OnMetarResponseReceived(object? sender, MetarResponseReceived e)
    {
        try
        {
            if (this.voiceServerConnection == null || this.networkConnection == null)
            {
                return;
            }

            if (this.NetworkConnectionStatus == NetworkConnectionStatus.Disconnected ||
                this.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                return;
            }

            if (this.SelectedAtisPreset == null)
            {
                return;
            }

            if (e.IsNewMetar)
            {
                this.IsNewAtis = false;
                if (!this.appConfig.SuppressNotificationSound)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }

                this.AcknowledgeOrIncrementAtisLetterCommand.Execute().Subscribe();
                this.IsNewAtis = true;
            }

            // Save the decoded metar so its individual properties can be sent to clients
            // connected via the websocket.
            this.decodedMetar = e.Metar;

            var propertyUpdates = new TaskCompletionSource();
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Metar = e.Metar.RawMetar?.ToUpperInvariant();
                    this.Altimeter = e.Metar.Pressure?.Value?.ActualUnit == Value.Unit.HectoPascal
                        ? "Q" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000")
                        : "A" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000");
                    this.Wind = e.Metar.SurfaceWind?.RawValue;
                    propertyUpdates.SetResult();
                });

            // Wait for the UI thread to finish updating the properties. Without this it's possible
            // to publish updated METAR information either via the hub or websocket with old data.
            await propertyUpdates.Task;

            if (this.atisStation.AtisVoice.UseTextToSpeech)
            {
                try
                {
                    // Cancel previous request
                    await this.cancellationToken.CancelAsync();
                    this.cancellationToken.Dispose();
                    this.cancellationToken = new CancellationTokenSource();

                    var atis = await this.atisBuilder.BuildAtis(
                        this.atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        e.Metar,
                        this.cancellationToken.Token);

                    this.atisStation.TextAtis = atis.TextAtis?.ToUpperInvariant();

                    await this.PublishAtisToHub();
                    this.networkConnection?.SendSubscriberNotification(this.AtisLetter);
                    await this.atisBuilder.UpdateIds(
                        this.atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        this.cancellationToken.Token);

                    if (atis.AudioBytes != null && this.networkConnection != null)
                    {
                        await Task.Run(
                            async () =>
                            {
                                var dto = AtisBotUtils.AddBotRequest(
                                    atis.AudioBytes,
                                    this.atisStation.Frequency,
                                    this.atisStationAirport.Latitude,
                                    this.atisStationAirport.Longitude,
                                    100);
                                await this.voiceServerConnection?.AddOrUpdateBot(
                                    this.networkConnection.Callsign,
                                    dto,
                                    this.cancellationToken.Token)!;
                            }).ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    this.ErrorMessage = string.Join(
                                        ",",
                                        t.Exception.InnerExceptions.Select(exception => exception.Message));
                                    this.networkConnection?.Disconnect();
                                    NativeAudio.EmitSound(SoundType.Error);
                                }
                            },
                            this.cancellationToken.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    this.ErrorMessage = ex.Message;
                    this.networkConnection?.Disconnect();
                    NativeAudio.EmitSound(SoundType.Error);
                }
            }

            // This is done at the very end to ensure the TextAtis is updated before the websocket message is sent.
            await this.PublishAtisToWebsocket();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = ex.Message;
                });
        }
    }

    /// <summary>
    ///     Publishes the current ATIS information to connected websocket clients.
    /// </summary>
    /// <param name="session">
    ///     The connected client to publish the data to. If omitted or null the data is broadcast to all
    ///     connected clients.
    /// </param>
    /// <returns>A task.</returns>
    private async Task PublishAtisToWebsocket(ClientMetadata? session = null)
    {
        await this.websocketService.SendAtisMessage(
            session,
            new AtisMessage.AtisMessageValue
            {
                Station = this.atisStation.Identifier,
                AtisType = this.atisStation.AtisType,
                AtisLetter = this.AtisLetter,
                Metar = this.Metar?.Trim(),
                Wind = this.Wind?.Trim(),
                Altimeter = this.Altimeter?.Trim(),
                TextAtis = this.atisStation.TextAtis,
                IsNewAtis = this.IsNewAtis,
                NetworkConnectionStatus = this.NetworkConnectionStatus,
                PressureUnit = this.decodedMetar?.Pressure?.Value?.ActualUnit,
                PressureValue = this.decodedMetar?.Pressure?.Value?.ActualValue,
            });
    }

    private async Task PublishAtisToHub()
    {
        await this.atisHubConnection.PublishAtis(
            new AtisHubDto(
                this.atisStation.Identifier,
                this.atisStation.AtisType,
                this.AtisLetter,
                this.Metar?.Trim(),
                this.Wind?.Trim(),
                this.Altimeter?.Trim()));

        // Setup timer to re-publish ATIS every 3 minutes to keep it active in the hub cache
        if (!this.isPublishAtisTriggeredInitially)
        {
            this.isPublishAtisTriggeredInitially = true;

            // ReSharper disable once AsyncVoidLambda
            this.publishAtisTimer = Observable.Interval(TimeSpan.FromMinutes(3)).Subscribe(
                async _ =>
                {
                    await this.atisHubConnection.PublishAtis(
                        new AtisHubDto(
                            this.atisStation.Identifier,
                            this.atisStation.AtisType,
                            this.AtisLetter,
                            this.Metar?.Trim(),
                            this.Wind?.Trim(),
                            this.Altimeter?.Trim()));
                });
        }
    }

    private async Task HandleSelectedAtisPresetChanged(AtisPreset? preset)
    {
        try
        {
            if (preset == null)
            {
                return;
            }

            if (preset != this.previousAtisPreset)
            {
                this.SelectedAtisPreset = preset;
                this.previousAtisPreset = preset;

                this.PopulateAirportConditions();
                this.PopulateNotams();

                this.HasUnsavedNotams = false;
                this.HasUnsavedAirportConditions = false;

                if (this.NetworkConnectionStatus != NetworkConnectionStatus.Connected ||
                    this.networkConnection == null)
                {
                    return;
                }

                if (this.decodedMetar == null)
                {
                    return;
                }

                var atis = await this.atisBuilder.BuildAtis(
                    this.atisStation,
                    this.SelectedAtisPreset,
                    this.AtisLetter,
                    this.decodedMetar,
                    this.cancellationToken.Token);

                this.atisStation.TextAtis = atis.TextAtis?.ToUpperInvariant();

                await this.PublishAtisToHub();
                await this.PublishAtisToWebsocket();
                await this.atisBuilder.UpdateIds(
                    this.atisStation,
                    this.SelectedAtisPreset,
                    this.AtisLetter,
                    this.cancellationToken.Token);

                if (this.atisStation.AtisVoice.UseTextToSpeech)
                {
                    // Cancel previous request
                    await this.cancellationToken.CancelAsync();
                    this.cancellationToken.Dispose();
                    this.cancellationToken = new CancellationTokenSource();

                    if (atis.AudioBytes != null)
                    {
                        await Task.Run(
                            async () =>
                            {
                                var dto = AtisBotUtils.AddBotRequest(
                                    atis.AudioBytes,
                                    this.atisStation.Frequency,
                                    this.atisStationAirport.Latitude,
                                    this.atisStationAirport.Longitude,
                                    100);
                                await this.voiceServerConnection?.AddOrUpdateBot(
                                    this.networkConnection.Callsign,
                                    dto,
                                    this.cancellationToken.Token)!;
                            },
                            this.cancellationToken.Token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = e.Message;
                });
        }
    }

    private void PopulateNotams()
    {
        if (this.NotamsTextDocument == null)
        {
            return;
        }

        // Clear the list of read-only NOTAM text segments.
        this.ReadOnlyNotams.Clear();

        // Retrieve and sort enabled static NOTAM definitions by their ordinal value.
        var staticDefinitions = this.atisStation.NotamDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        this.NotamsTextDocument.Text = string.Empty;

        // Reset offset
        this.notamFreeTextOffset = 0;

        // If static NOTAM definitions exist, insert them into the document.
        if (staticDefinitions.Count > 0)
        {
            // Combine static NOTAM definitions into a single string, separated by periods.
            var staticDefinitionsString = string.Join(". ", staticDefinitions) + ". ";

            // Insert static NOTAM definitions at the beginning of the document.
            this.NotamsTextDocument.Insert(0, staticDefinitionsString);

            // Add the static NOTAM range to the read-only list to prevent modification.
            this.ReadOnlyNotams.Add(
                new TextSegment
                {
                    StartOffset = 0,
                    EndOffset = staticDefinitionsString.Length,
                });

            // Update the starting index for the next insertion.
            this.notamFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form NOTAM text after the static definitions (if any).
        if (!string.IsNullOrEmpty(this.SelectedAtisPreset?.Notams))
        {
            this.NotamsTextDocument.Insert(this.notamFreeTextOffset, this.SelectedAtisPreset?.Notams);
        }
    }

    private void PopulateAirportConditions()
    {
        if (this.AirportConditionsTextDocument == null)
        {
            return;
        }

        // Clear the list of read-only NOTAM text segments.
        this.ReadOnlyAirportConditions.Clear();

        // Retrieve and sort enabled static airport conditions by their ordinal value.
        var staticDefinitions = this.atisStation.AirportConditionDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        this.AirportConditionsTextDocument.Text = string.Empty;

        // Reset offset
        this.airportConditionsFreeTextOffset = 0;

        // If static airport conditions exist, insert them into the document.
        if (staticDefinitions.Count > 0)
        {
            // Combine static airport conditions into a single string, separated by periods.
            // A trailing space is added to ensure proper spacing between the static definitions
            // and the subsequent free-form text.
            var staticDefinitionsString = string.Join(". ", staticDefinitions) + ". ";

            // Insert static airport conditions at the beginning of the document.
            this.AirportConditionsTextDocument.Insert(0, staticDefinitionsString);

            // Add the static airport conditions to the read-only list to prevent modification.
            this.ReadOnlyAirportConditions.Add(
                new TextSegment
                {
                    StartOffset = 0,
                    EndOffset = staticDefinitionsString.Length,
                });

            // Update the starting index for the next insertion.
            this.airportConditionsFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form airport conditions after the static definitions (if any).
        if (!string.IsNullOrEmpty(this.SelectedAtisPreset?.AirportConditions))
        {
            this.AirportConditionsTextDocument.Insert(
                this.airportConditionsFreeTextOffset,
                this.SelectedAtisPreset?.AirportConditions);
        }
    }

    private async void HandleIsNewAtisChanged(bool isNew)
    {
        try
        {
            await this.PublishAtisToWebsocket();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in HandleIsNewAtisChanged");
        }
    }

    private async void HandleAtisLetterChanged(char letter)
    {
        try
        {
            // Always publish the latest information to the websocket, even if the station isn't
            // connected or doesn't support text to speech.
            await this.PublishAtisToWebsocket();

            if (!this.atisStation.AtisVoice.UseTextToSpeech)
            {
                return;
            }

            if (this.NetworkConnectionStatus != NetworkConnectionStatus.Connected)
            {
                return;
            }

            if (this.SelectedAtisPreset == null)
            {
                return;
            }

            if (this.networkConnection == null || this.voiceServerConnection == null)
            {
                return;
            }

            if (this.decodedMetar == null)
            {
                return;
            }

            // Cancel previous request
            await this.cancellationToken.CancelAsync();
            this.cancellationToken.Dispose();
            this.cancellationToken = new CancellationTokenSource();

            await Task.Run(
                async () =>
                {
                    try
                    {
                        var atis = await this.atisBuilder.BuildAtis(
                            this.atisStation,
                            this.SelectedAtisPreset,
                            this.atisLetter,
                            this.decodedMetar,
                            this.cancellationToken.Token);

                        this.atisStation.TextAtis = atis.TextAtis?.ToUpperInvariant();

                        await this.PublishAtisToHub();
                        this.networkConnection?.SendSubscriberNotification(this.AtisLetter);
                        await this.atisBuilder.UpdateIds(
                            this.atisStation,
                            this.SelectedAtisPreset,
                            this.AtisLetter,
                            this.cancellationToken.Token);

                        if (atis.AudioBytes != null && this.networkConnection != null)
                        {
                            var dto = AtisBotUtils.AddBotRequest(
                                atis.AudioBytes,
                                this.atisStation.Frequency,
                                this.atisStationAirport.Latitude,
                                this.atisStationAirport.Longitude,
                                100);
                            this.voiceServerConnection?.AddOrUpdateBot(
                                this.networkConnection.Callsign,
                                dto,
                                this.cancellationToken.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.UIThread.Post(() => { this.ErrorMessage = ex.Message; });
                    }
                },
                this.cancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.Wind = null;
                    this.Altimeter = null;
                    this.Metar = null;
                    this.ErrorMessage = e.Message;
                });
        }
    }

    private async void OnGetAtisReceived(object? sender, GetAtisReceived e)
    {
        try
        {
            // If a specific station is specified then both the station identifier and the ATIS type
            // must match to acknowledge the update.
            // If a specific station isn't specified then the request is for all stations.
            if (!string.IsNullOrEmpty(e.Station) &&
                (e.Station != this.atisStation.Identifier || e.AtisType != this.atisStation.AtisType))
            {
                return;
            }

            await this.PublishAtisToWebsocket(e.Session);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in OnGetAtisReceived");
        }
    }

    private void OnAcknowledgeAtisUpdateReceived(object? sender, AcknowledgeAtisUpdateReceived e)
    {
        // If a specific station is specified then both the station identifier and the ATIS type
        // must match to acknowledge the update.
        // If a specific station isn't specified then the request is for all stations.
        if (!string.IsNullOrEmpty(e.Station) &&
            (e.Station != this.atisStation.Identifier || e.AtisType != this.atisStation.AtisType))
        {
            return;
        }

        this.HandleAcknowledgeAtisUpdate();
    }

    private void HandleAcknowledgeAtisUpdate()
    {
        if (this.IsNewAtis)
        {
            this.IsNewAtis = false;
        }
    }

    private void AcknowledgeOrIncrementAtisLetter()
    {
        if (this.IsNewAtis)
        {
            this.IsNewAtis = false;
            return;
        }

        this.AtisLetter++;
        if (this.AtisLetter > this.atisStation.CodeRange.High)
        {
            this.AtisLetter = this.atisStation.CodeRange.Low;
        }
    }

    private void DecrementAtisLetter()
    {
        this.AtisLetter--;
        if (this.AtisLetter < this.atisStation.CodeRange.Low)
        {
            this.AtisLetter = this.atisStation.CodeRange.High;
        }
    }
}
