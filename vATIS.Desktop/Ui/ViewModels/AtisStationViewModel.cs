// <copyright file="AtisStationViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Container.Factory;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Events.WebSocket;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Ui.Services.Websocket;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Voice.Utils;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Extensions;
using WatsonWebsocket;
using Notification = Vatsim.Vatis.Ui.Controls.Notification.Notification;
using WindowNotificationManager = Vatsim.Vatis.Ui.Controls.Notification.WindowNotificationManager;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a ViewModel for managing ATIS station information and operations.
/// </summary>
public class AtisStationViewModel : ReactiveViewModelBase, IDisposable
{
    private const int TransceiverHeightM = 10;
    private readonly IAppConfig _appConfig;
    private readonly IProfileRepository _profileRepository;
    private readonly IAtisBuilder _atisBuilder;
    private readonly IWindowFactory _windowFactory;
    private readonly INetworkConnection? _networkConnection;
    private readonly IVoiceServerConnection? _voiceServerConnection;
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly IWebsocketService _websocketService;
    private readonly ISessionManager _sessionManager;
    private readonly Airport _atisStationAirport;
    private readonly MetarDecoder _metarDecoder = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly SemaphoreSlim _voiceRequestLock = new(1, 1);
    private readonly SemaphoreSlim _buildAtisLock = new(1, 1);
    private readonly string? _identifier;
    private CancellationTokenSource _buildAtisCts = new();
    private CancellationTokenSource _selectedPresetCts = new();
    private CancellationTokenSource _voiceRecordAtisCts = new();
    private CancellationTokenSource _processMetarCts = new();
    private CancellationTokenSource _atisLetterChangedCts = new();
    private AtisPreset? _previousAtisPreset;
    private DecodedMetar? _decodedMetar;
    private Timer? _publishAtisTimer;
    private int _notamFreeTextOffset;
    private int _airportConditionsFreeTextOffset;
    private string? _id;
    private string? _tabText;
    private int _ordinal;
    private char _atisLetter;
    private bool _isAtisLetterInputMode;
    private string? _metarString;
    private string? _observationTime;
    private string? _wind;
    private string? _altimeter;
    private bool _isNewAtis;
    private string _atisTypeLabel = "";
    private bool _isCombinedAtis;
    private ObservableCollection<AtisPreset> _atisPresetList = [];
    private AtisPreset? _selectedAtisPreset;
    private TextDocument? _airportConditionsTextDocument = new();
    private TextDocument? _notamsTextDocument = new();
    private bool _useTexToSpeech;
    private RecordedAtisState _recordedAtisState = RecordedAtisState.Disconnected;
    private NetworkConnectionStatus _networkConnectionStatus = NetworkConnectionStatus.Disconnected;
    private List<ICompletionData> _contractionCompletionData = [];
    private bool _hasUnsavedAirportConditions;
    private bool _hasUnsavedNotams;
    private string? _previousFreeTextNotams;
    private string? _previousFreeTextAirportConditions;
    private bool _isVisibleOnMiniWindow = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisStationViewModel"/> class.
    /// </summary>
    /// <param name="station">
    /// The ATIS station instance associated with this view model.
    /// </param>
    /// <param name="windowNotificationManager">The notification manager used for displaying notifications.</param>
    /// <param name="connectionFactory">
    /// The network connection factory used to manage network connections.
    /// </param>
    /// <param name="voiceServerConnectionFactory">
    /// The voice server connection factory used to manage voice server connections.
    /// </param>
    /// <param name="appConfig">
    /// The application configuration used for accessing app settings.
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
    public AtisStationViewModel(AtisStation station, WindowNotificationManager? windowNotificationManager,
        INetworkConnectionFactory connectionFactory, IVoiceServerConnectionFactory voiceServerConnectionFactory,
        IAppConfig appConfig, IAtisBuilder atisBuilder, IWindowFactory windowFactory,
        INavDataRepository navDataRepository, IAtisHubConnection hubConnection, ISessionManager sessionManager,
        IProfileRepository profileRepository, IWebsocketService websocketService)
    {
        Id = station.Id;
        Identifier = station.Identifier;
        Ordinal = station.Ordinal;
        AtisStation = station;
        _appConfig = appConfig;
        _atisBuilder = atisBuilder;
        _windowFactory = windowFactory;
        _websocketService = websocketService;
        _atisHubConnection = hubConnection;
        _sessionManager = sessionManager;
        _profileRepository = profileRepository;
        _atisStationAirport = navDataRepository.GetAirport(station.Identifier) ??
                              throw new ApplicationException($"{station.Identifier} not found in airport navdata.");

        _atisLetter = AtisStation.CodeRange.Low;

        ReadOnlyAirportConditions = new TextSegmentCollection<TextSegment>(AirportConditionsTextDocument);
        ReadOnlyNotams = new TextSegmentCollection<TextSegment>(NotamsTextDocument);
        NotificationManager = windowNotificationManager;

        switch (station.AtisType)
        {
            case AtisType.Arrival:
                TabText = $"{Identifier}/A";
                AtisTypeLabel = "ARR";
                break;
            case AtisType.Departure:
                TabText = $"{Identifier}/D";
                AtisTypeLabel = "DEP";
                break;
            case AtisType.Combined:
                TabText = Identifier;
                AtisTypeLabel = "";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        IsCombinedAtis = station.AtisType == AtisType.Combined;
        AtisPresetList = new ObservableCollection<AtisPreset>(station.Presets.OrderBy(x => x.Ordinal));

        OpenStaticAirportConditionsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenAirportConditionsDialog);
        OpenStaticNotamsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenStaticNotamsDialog);

        SetAtisLetterCommand = ReactiveCommand.Create<char>(HandleSetAtisLetter);
        ApplyAirportConditionsCommand = ReactiveCommand.Create<bool>(HandleApplyAirportConditions);
        ApplyNotamsCommand = ReactiveCommand.Create<bool>(HandleApplyNotams);
        SelectedPresetChangedCommand = ReactiveCommand.CreateFromTask<AtisPreset>(HandleSelectedAtisPresetChanged);
        AcknowledgeAtisUpdateCommand = ReactiveCommand.Create<PointerPressedEventArgs>(HandleAcknowledgeAtisUpdate);
        DecrementAtisLetterCommand = ReactiveCommand.Create(DecrementAtisLetter);
        AcknowledgeOrIncrementAtisLetterCommand = ReactiveCommand.Create(AcknowledgeOrIncrementAtisLetter);
        NetworkConnectCommand = ReactiveCommand.CreateFromTask(HandleNetworkConnect, this.WhenAnyValue(
            x => x.SelectedAtisPreset,
            x => x.NetworkConnectionStatus,
            (atisPreset, networkStatus) => atisPreset != null && networkStatus != NetworkConnectionStatus.Connecting));
        VoiceRecordAtisCommand = ReactiveCommand.CreateFromTask(HandleVoiceRecordAtisCommand,
            this.WhenAnyValue(
                x => x.Metar,
                x => x.UseTexToSpeech,
                x => x.NetworkConnectionStatus,
                (metar, voiceRecord, networkStatus) => !string.IsNullOrEmpty(metar) && voiceRecord &&
                                                       networkStatus == NetworkConnectionStatus.Connected));

        this.WhenAnyValue(x => x.AirportConditionsTextDocument!.Text)
            .Throttle(TimeSpan.FromSeconds(5))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ =>
            {
                // Apply but don't save to profile
                ApplyAirportConditionsCommand.Execute(false).Subscribe();
            }).DisposeWith(_disposables);

        this.WhenAnyValue(x => x.NotamsTextDocument!.Text)
            .Throttle(TimeSpan.FromSeconds(5))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ =>
            {
                // Apply but don't save to profile
                ApplyNotamsCommand.Execute(false).Subscribe();
            }).DisposeWith(_disposables);

        _websocketService.GetAtisReceived += OnGetAtisReceived;
        _websocketService.AcknowledgeAtisUpdateReceived += OnAcknowledgeAtisUpdateReceived;
        _websocketService.ConfigureAtisReceived += OnConfigureAtisReceived;
        _websocketService.ConnectAtisReceived += OnConnectAtisReceived;
        _websocketService.DisconnectAtisReceived += OnDisconnectAtisReceived;

        LoadContractionData();

        _networkConnection = connectionFactory.CreateConnection(AtisStation);
        _networkConnection.NetworkConnectionFailed += OnNetworkConnectionFailed;
        _networkConnection.NetworkErrorReceived += OnNetworkErrorReceived;
        _networkConnection.NetworkConnected += OnNetworkConnected;
        _networkConnection.NetworkDisconnected += OnNetworkDisconnected;
        _networkConnection.ChangeServerReceived += OnChangeServerReceived;
        _networkConnection.MetarResponseReceived += OnMetarResponseReceived;
        _networkConnection.KillRequestReceived += OnKillRequestedReceived;
        _voiceServerConnection = voiceServerConnectionFactory.CreateVoiceServerConnection();

        UseTexToSpeech = !AtisStation.AtisVoice.UseTextToSpeech;
        _disposables.Add(EventBus.Instance.Subscribe<AcknowledgeAllAtisUpdates>(_ =>
        {
            AcknowledgeAtisUpdateCommand.Execute().Subscribe();
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisVoiceTypeChanged>(evt =>
        {
            if (evt.Id == AtisStation.Id)
            {
                UseTexToSpeech = !evt.UseTextToSpeech;
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<StationPresetsChanged>(evt =>
        {
            if (evt.Id == AtisStation.Id)
            {
                AtisPresetList = [.. AtisStation.Presets.OrderBy(x => x.Ordinal)];
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<ContractionsUpdated>(evt =>
        {
            if (evt.StationId == AtisStation.Id)
            {
                LoadContractionData();
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisHubAtisReceived>(sync =>
        {
            if (sync.Dto.StationId != station.Identifier || sync.Dto.AtisType != station.AtisType)
                return;

            if (NetworkConnectionStatus == NetworkConnectionStatus.Connected)
                return;

            if (!string.IsNullOrEmpty(sync.Dto.Metar))
            {
                _decodedMetar = _metarDecoder.ParseNotStrict(sync.Dto.Metar);
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                {
                    var atisLetterChanged = sync.Dto.AtisLetter != AtisLetter;
                    var metarChanged = !string.IsNullOrEmpty(sync.Dto.Metar) &&
                                       !string.Equals(sync.Dto.Metar, Metar, StringComparison.OrdinalIgnoreCase);

                    if (atisLetterChanged || metarChanged)
                    {
                        IsNewAtis = true;

                        if (!_appConfig.MuteSharedAtisUpdateSound && IsVisibleOnMiniWindow)
                        {
                            NativeAudio.EmitSound(SoundType.Notification);
                        }
                    }
                }

                AtisLetter = sync.Dto.AtisLetter;
                Wind = sync.Dto.Wind;
                Altimeter = sync.Dto.Altimeter;
                Metar = sync.Dto.Metar;
                ObservationTime = _decodedMetar?.Time.Replace(":", "");
                AtisStation.TextAtis = sync.Dto.TextAtis;
                NetworkConnectionStatus = sync.Dto.IsOnline
                    ? NetworkConnectionStatus.Observer
                    : NetworkConnectionStatus.Disconnected;

                // Sync airport conditions and NOTAM text for online ATISes
                if (sync.Dto.IsOnline)
                {
                    if (AirportConditionsTextDocument != null)
                    {
                        AirportConditionsTextDocument.Text = sync.Dto.AirportConditions ?? "";
                    }

                    if (NotamsTextDocument != null)
                    {
                        NotamsTextDocument.Text = sync.Dto.Notams ?? "";
                    }
                }
                else
                {
                    // Clear Airport Conditions and NOTAMs
                    if (AirportConditionsTextDocument != null)
                    {
                        AirportConditionsTextDocument.Text = "";
                    }

                    if (NotamsTextDocument != null)
                    {
                        NotamsTextDocument.Text = "";
                    }
                }
            });
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisHubExpiredAtisReceived>(sync =>
        {
            if (sync.Dto.StationId == AtisStation.Identifier &&
                sync.Dto.AtisType == AtisStation.AtisType &&
                NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Validate ATIS letter to make sure it's within the configured code range.
                    AtisLetter = sync.Dto.AtisLetter < AtisStation.CodeRange.Low ||
                                 sync.Dto.AtisLetter > AtisStation.CodeRange.High
                        ? AtisStation.CodeRange.Low
                        : sync.Dto.AtisLetter;
                    Wind = null;
                    Altimeter = null;
                    Metar = null;
                    ObservationTime = null;
                    NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                    IsNewAtis = false;
                    AtisStation.TextAtis = null;

                    // Clear Airport Conditions and NOTAMs
                    if (AirportConditionsTextDocument != null)
                    {
                        AirportConditionsTextDocument.Text = "";
                    }

                    if (NotamsTextDocument != null)
                    {
                        NotamsTextDocument.Text = "";
                    }
                });
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<HubConnected>(_ => { SubscribeToAtis(); }));
        _disposables.Add(EventBus.Instance.Subscribe<SessionEnded>(_ =>
        {
            if (NetworkConnectionStatus == NetworkConnectionStatus.Connected)
            {
                _atisHubConnection.DisconnectAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                    AtisLetter));

                _voiceServerConnection.RemoveBot(_networkConnection.Callsign);
            }

            _voiceServerConnection.Disconnect();
            _networkConnection.Disconnect();
        }));

        this.WhenAnyValue(x => x.IsNewAtis).Subscribe(HandleIsNewAtisChanged);

        this.WhenAnyValue(x => x.AtisLetter)
            .Select(_ => Observable.FromAsync(() => PublishAtisToWebsocket()))
            .Concat()
            .Subscribe();

        this.WhenAnyValue(x => x.AtisLetter)
            .Skip(1)
            .Throttle(TimeSpan.FromSeconds(5))
            .Select(_ => Observable.FromAsync(HandleAtisLetterChanged))
            .Concat()
            .Subscribe(_ => { }, ex => Log.Error(ex, "Error in HandleAtisLetterChanged"));

        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1)
            .Select(status => Observable.FromAsync(() => HandleNetworkStatusChanged(status)))
            .Concat()
            .Subscribe(_ => { }, ex => Log.Error(ex, "Error in HandleNetworkStatusChanged"));
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
    public ReactiveCommand<PointerPressedEventArgs, Unit> AcknowledgeAtisUpdateCommand { get; }

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
    /// Gets the command that updates the ATIS with the changed airport conditions.
    /// </summary>
    /// <remarks>
    /// Pass <c>true</c> to save the changes to the profile; <c>false</c> to apply them temporarily.
    /// </remarks>
    public ReactiveCommand<bool, Unit> ApplyAirportConditionsCommand { get; }

    /// <summary>
    /// Gets the command that updates the ATIS with the changed NOTAMs.
    /// </summary>
    /// <remarks>
    /// Pass <c>true</c> to save the changes to the profile; <c>false</c> to apply them temporarily.
    /// </remarks>
    public ReactiveCommand<bool, Unit> ApplyNotamsCommand { get; }

    /// <summary>
    /// Gets the ATIS station.
    /// </summary>
    public AtisStation AtisStation { get; }

    /// <summary>
    /// Gets the unique identifier for the ATIS station.
    /// </summary>
    public string? Id
    {
        get => _id;
        private set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// Gets the identifier associated with the ATIS station.
    /// </summary>
    public string? Identifier
    {
        get => _identifier;
        private init => this.RaiseAndSetIfChanged(ref _identifier, value);
    }

    /// <summary>
    /// Gets or sets the text displayed on the tab for the ATIS station.
    /// </summary>
    public string? TabText
    {
        get => _tabText;
        set => this.RaiseAndSetIfChanged(ref _tabText, value);
    }

    /// <summary>
    /// Gets or sets the ATIS letter associated with the station.
    /// </summary>
    public char AtisLetter
    {
        get => _atisLetter;
        set => this.RaiseAndSetIfChanged(ref _atisLetter, value);
    }

    /// <summary>
    /// Gets the range of valid ATIS code letters associated with the ATIS station.
    /// </summary>
    public CodeRangeMeta CodeRange => AtisStation.CodeRange;

    /// <summary>
    /// Gets or sets a value indicating whether the ATIS letter input mode is active.
    /// </summary>
    public bool IsAtisLetterInputMode
    {
        get => _isAtisLetterInputMode;
        set => this.RaiseAndSetIfChanged(ref _isAtisLetterInputMode, value);
    }

    /// <summary>
    /// Gets or sets the observation time of the METAR.
    /// </summary>
    public string? ObservationTime
    {
        get => _observationTime;
        set => this.RaiseAndSetIfChanged(ref _observationTime, value);
    }

    /// <summary>
    /// Gets or sets the METAR string for the ATIS station.
    /// </summary>
    public string? Metar
    {
        get => _metarString;
        set => this.RaiseAndSetIfChanged(ref _metarString, value);
    }

    /// <summary>
    /// Gets or sets the wind information associated with the ATIS station.
    /// </summary>
    public string? Wind
    {
        get => _wind;
        set => this.RaiseAndSetIfChanged(ref _wind, value?.Trim());
    }

    /// <summary>
    /// Gets or sets the altimeter value as a string representation.
    /// </summary>
    public string? Altimeter
    {
        get => _altimeter;
        set => this.RaiseAndSetIfChanged(ref _altimeter, value?.Trim());
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ATIS is new.
    /// </summary>
    public bool IsNewAtis
    {
        get => _isNewAtis;
        set => this.RaiseAndSetIfChanged(ref _isNewAtis, value);
    }

    /// <summary>
    /// Gets or sets the ATIS type label.
    /// </summary>
    public string AtisTypeLabel
    {
        get => _atisTypeLabel;
        set => this.RaiseAndSetIfChanged(ref _atisTypeLabel, value);
    }

    /// <summary>
    /// Gets a value indicating whether the ATIS station type is "Combined".
    /// </summary>
    public bool IsCombinedAtis
    {
        get => _isCombinedAtis;
        private set => this.RaiseAndSetIfChanged(ref _isCombinedAtis, value);
    }

    /// <summary>
    /// Gets or sets the collection of ATIS presets.
    /// </summary>
    public ObservableCollection<AtisPreset> AtisPresetList
    {
        get => _atisPresetList;
        set => this.RaiseAndSetIfChanged(ref _atisPresetList, value);
    }

    /// <summary>
    /// Gets the currently selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedAtisPreset
    {
        get => _selectedAtisPreset;
        private set => this.RaiseAndSetIfChanged(ref _selectedAtisPreset, value);
    }

    /// <summary>
    /// Gets the free text representation of the airport conditions.
    /// </summary>
    public string? AirportConditionsFreeText => AirportConditionsTextDocument?.Text;

    /// <summary>
    /// Gets or sets the text document containing airport conditions.
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
    /// Gets or sets the NOTAMs text document for editing operations.
    /// </summary>
    public TextDocument? NotamsTextDocument
    {
        get => _notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _notamsTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the network connection status of the ATIS station.
    /// </summary>
    public NetworkConnectionStatus NetworkConnectionStatus
    {
        get => _networkConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref _networkConnectionStatus, value);
    }

    /// <summary>
    /// Gets or sets the status of a manually recorded ATIS.
    /// </summary>
    public RecordedAtisState RecordedAtisState
    {
        get => _recordedAtisState;
        set => this.RaiseAndSetIfChanged(ref _recordedAtisState, value);
    }

    /// <summary>
    /// Gets or sets the collection of contraction completion data used for auto-completion.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes to the airport conditions.
    /// </summary>
    public bool HasUnsavedAirportConditions
    {
        get => _hasUnsavedAirportConditions;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedAirportConditions, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes to the NOTAMs.
    /// </summary>
    public bool HasUnsavedNotams
    {
        get => _hasUnsavedNotams;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedNotams, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the station is visible on the mini-window.
    /// </summary>
    public bool IsVisibleOnMiniWindow
    {
        get => _isVisibleOnMiniWindow;
        set => this.RaiseAndSetIfChanged(ref _isVisibleOnMiniWindow, value);
    }

    /// <summary>
    /// Gets a value indicating the ATIS type.
    /// </summary>
    public AtisType AtisType => AtisStation.AtisType;

    /// <summary>
    /// Gets or sets a value indicating the station sort ordinal.
    /// </summary>
    public int Ordinal
    {
        get => _ordinal;
        set => this.RaiseAndSetIfChanged(ref _ordinal, value);
    }

    private ReactiveCommand<char, Unit> SetAtisLetterCommand { get; }

    private WindowNotificationManager? NotificationManager { get; }

    private bool UseTexToSpeech
    {
        get => _useTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref _useTexToSpeech, value);
    }

    /// <summary>
    /// Disconnects the current network connection and updates the network connection status
    /// to <see cref="NetworkConnectionStatus.Disconnected"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Disconnect()
    {
        if (_networkConnection != null)
        {
            // Disconnect voice ATIS
            _voiceServerConnection?.RemoveBot(_networkConnection.Callsign);
            _voiceServerConnection?.Disconnect();

            // Disconnect from network
            _networkConnection?.Disconnect();
        }

        // Flag ATIS as disconnected on the hub
        try
        {
            await _atisHubConnection.DisconnectAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                AtisLetter));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to disconnect ATIS from hub.");
        }

        // Set network connection status as disconnected
        NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
        RecordedAtisState = RecordedAtisState.Disconnected;
    }

    /// <summary>
    /// Subscribes to ATIS on hub server.
    /// </summary>
    public void SubscribeToAtis()
    {
        _atisHubConnection.SubscribeToAtis(new SubscribeDto(AtisStation.Identifier, AtisStation.AtisType));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        _websocketService.GetAtisReceived -= OnGetAtisReceived;
        _websocketService.AcknowledgeAtisUpdateReceived -= OnAcknowledgeAtisUpdateReceived;
        _websocketService.ConfigureAtisReceived -= OnConfigureAtisReceived;
        _websocketService.ConnectAtisReceived -= OnConnectAtisReceived;
        _websocketService.DisconnectAtisReceived -= OnDisconnectAtisReceived;

        if (_networkConnection != null)
        {
            _networkConnection.NetworkConnectionFailed -= OnNetworkConnectionFailed;
            _networkConnection.NetworkErrorReceived -= OnNetworkErrorReceived;
            _networkConnection.NetworkConnected -= OnNetworkConnected;
            _networkConnection.NetworkDisconnected -= OnNetworkDisconnected;
            _networkConnection.ChangeServerReceived -= OnChangeServerReceived;
            _networkConnection.MetarResponseReceived -= OnMetarResponseReceived;
            _networkConnection.KillRequestReceived -= OnKillRequestedReceived;
        }

        _publishAtisTimer?.Dispose();
        _publishAtisTimer = null;

        DecrementAtisLetterCommand.Dispose();
        AcknowledgeOrIncrementAtisLetterCommand.Dispose();
        AcknowledgeAtisUpdateCommand.Dispose();
        NetworkConnectCommand.Dispose();
        VoiceRecordAtisCommand.Dispose();
        OpenStaticAirportConditionsDialogCommand.Dispose();
        OpenStaticNotamsDialogCommand.Dispose();
        SelectedPresetChangedCommand.Dispose();
        ApplyAirportConditionsCommand.Dispose();
        ApplyNotamsCommand.Dispose();

        // Dispose cancellation tokens
        _processMetarCts.Dispose();
        _voiceRecordAtisCts.Dispose();
        _selectedPresetCts.Dispose();
        _atisLetterChangedCts.Dispose();
        _buildAtisCts.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleSetAtisLetter(char letter)
    {
        if (letter < AtisStation.CodeRange.Low || letter > AtisStation.CodeRange.High)
            return;
        AtisLetter = letter;
    }

    private void HandleApplyAirportConditions(bool saveToProfile)
    {
        ApplyAirportConditions(saveToProfile);
        HasUnsavedAirportConditions = !saveToProfile;
    }

    private void HandleApplyNotams(bool saveToProfile)
    {
        ApplyNotams(saveToProfile);
        HasUnsavedNotams = !saveToProfile;
    }

    private void ApplyNotams(bool saveToProfile)
    {
        if (SelectedAtisPreset == null)
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

        SelectedAtisPreset.Notams = freeText?.Trim();

        if (saveToProfile && _sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        // Cancel previous request
        _buildAtisCts.Cancel();
        _buildAtisCts = new CancellationTokenSource();
        var localToken = _buildAtisCts.Token;

        BuildAtis(localToken, notifySubscribers: false).SafeFireAndForget(onException: exception =>
        {
            NotificationManager?.Show(
                new Notification("Error Building ATIS", "See log for details: " + exception.Message),
                NotificationType.Error,
                expiration: TimeSpan.Zero
            );
            Log.Error(exception, "BuildAtis Exception");
        });
    }

    private void ApplyAirportConditions(bool saveToProfile)
    {
        if (SelectedAtisPreset == null)
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

        SelectedAtisPreset.AirportConditions = freeText?.Trim();

        if (saveToProfile && _sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        // Cancel previous request
        _buildAtisCts.Cancel();
        _buildAtisCts = new CancellationTokenSource();
        var localToken = _buildAtisCts.Token;

        BuildAtis(localToken, notifySubscribers: false).SafeFireAndForget(onException: exception =>
        {
            NotificationManager?.Show(
                new Notification("Error Building ATIS", "See log for details: " + exception.Message),
                NotificationType.Error,
                expiration: TimeSpan.Zero
            );
            Log.Error(exception, "BuildAtis Exception");
        });
    }

    private void LoadContractionData()
    {
        ContractionCompletionData.Clear();

        foreach (var contraction in AtisStation.Contractions.ToList())
        {
            if (contraction is { VariableName: not null, Voice: not null })
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
        }
    }

    private async Task HandleOpenStaticNotamsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        var dlg = _windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(AtisStation.NotamDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;
            viewModel.IncludeBeforeFreeText = AtisStation.NotamsBeforeFreeText;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                AtisStation.NotamsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    AtisStation.NotamDefinitions.Clear();
                    AtisStation.NotamDefinitions.AddRange(changes);
                    if (_sessionManager.CurrentProfile != null)
                        _profileRepository.Save(_sessionManager.CurrentProfile);
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                AtisStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    AtisStation.NotamDefinitions.Add(item);
                }

                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        if (NetworkConnectionStatus != NetworkConnectionStatus.Observer)
        {
            // Update the free-form text area after the dialog is closed
            PopulateNotams();
        }
    }

    private async Task HandleOpenAirportConditionsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        var dlg = _windowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(AtisStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;
            viewModel.IncludeBeforeFreeText = AtisStation.AirportConditionsBeforeFreeText;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                AtisStation.AirportConditionsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    AtisStation.AirportConditionDefinitions.Clear();
                    AtisStation.AirportConditionDefinitions.AddRange(changes);
                    if (_sessionManager.CurrentProfile != null)
                        _profileRepository.Save(_sessionManager.CurrentProfile);
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                AtisStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    AtisStation.AirportConditionDefinitions.Add(item);
                }

                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            };
        }

        await dlg.ShowDialog(lifetime.MainWindow);

        if (NetworkConnectionStatus != NetworkConnectionStatus.Observer)
        {
            // Update the free-form text area after the dialog is closed
            PopulateAirportConditions();
        }
    }

    private void OnKillRequestedReceived(object? sender, KillRequestReceived e)
    {
        NativeAudio.EmitSound(SoundType.Error);

        NotificationManager?.Show(
            new Notification("Disconnected", string.IsNullOrEmpty(e.Reason)
                ? $"{_networkConnection?.Callsign} forcefully disconnected from the network."
                : $"{_networkConnection?.Callsign} forcefully disconnected from network.\nReason: {e.Reason}"),
            type: NotificationType.Error,
            expiration: TimeSpan.Zero
        );

        Dispatcher.UIThread.Post(() =>
        {
            Wind = null;
            Altimeter = null;
            Metar = null;
            ObservationTime = null;
        });
    }

    private async Task HandleVoiceRecordAtisCommand()
    {
        try
        {
            if (SelectedAtisPreset == null || _networkConnection == null || _voiceServerConnection == null ||
                _decodedMetar == null)
                return;

            // Cancel previous request
            await _voiceRecordAtisCts.CancelAsync();
            _voiceRecordAtisCts = new CancellationTokenSource();
            var localToken = _voiceRecordAtisCts;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                    return;

                var window = _windowFactory.CreateVoiceRecordAtisDialog();
                if (window.DataContext is VoiceRecordAtisDialogViewModel vm)
                {
                    var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        _decodedMetar, localToken.Token);

                    vm.AtisScript = textAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        try
                        {
                            AtisStation.TextAtis = textAtis;
                            AtisStation.AtisLetter = AtisLetter;

                            // Publish the ATIS to the hub
                            await PublishAtisToHub();

                            // Notify all subscribed users about the new ATIS update
                            _networkConnection.SendSubscriberNotification(AtisLetter);

                            // Update IDS
                            await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter,
                                localToken.Token);

                            // Generate DTO with ATIS audio and transceiver data
                            var dto = AtisBotUtils.CreateAtisBotDto(vm.AudioBuffer, AtisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, TransceiverHeightM);

                            // Send the DTO to the voice server
                            await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                localToken.Token);

                            RecordedAtisState = RecordedAtisState.Connected;
                            IsNewAtis = false;
                        }
                        catch (OperationCanceledException)
                        {
                            // Swallow cancellation, since it is expected behavior
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error in voice ATIS update");

                            NotificationManager?.Show(
                                new Notification("Voice ATIS Error", ex.Message),
                                type: NotificationType.Error,
                                expiration: TimeSpan.Zero
                            );

                            await Disconnect();
                            NativeAudio.EmitSound(SoundType.Error);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation, since it is expected behavior
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleVoiceRecordAtisCommand Exception");

            NotificationManager?.Show(
                new Notification("Voice Record ATIS Error", e.Message),
                type: NotificationType.Error,
                expiration: TimeSpan.Zero
            );

            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    private async Task HandleNetworkStatusChanged(NetworkConnectionStatus status)
    {
        try
        {
            if (_voiceServerConnection == null || _networkConnection == null)
                return;

            await PublishAtisToWebsocket();

            switch (status)
            {
                case NetworkConnectionStatus.Connected:
                    await ConnectToVoiceServer();
                    break;
                case NetworkConnectionStatus.Disconnected:
                    await DisconnectFromVoiceServer();

                    // Reset airport and NOTAM textboxes
                    PopulateNotams(true);
                    PopulateAirportConditions(true);
                    break;
                case NetworkConnectionStatus.Connecting:
                case NetworkConnectionStatus.Observer:
                    break;
                default:
                    throw new ApplicationException("Unknown network connection status: " + status);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleNetworkStatusChanged Exception");

            NotificationManager?.Show(
                new Notification("Error", e.Message),
                type: NotificationType.Error,
                expiration: TimeSpan.Zero
            );

            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    private async Task ConnectToVoiceServer()
    {
        try
        {
            if (_voiceServerConnection != null)
            {
                await _voiceServerConnection.Connect();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HandleConnectedStatus Exception");
            NotificationManager?.Show(
                new Notification("Error", ex.Message),
                type: NotificationType.Error,
                expiration: TimeSpan.Zero
            );
        }
    }

    private async Task DisconnectFromVoiceServer()
    {
        if (_voiceServerConnection == null || _networkConnection == null)
            return;

        try
        {
            if (NetworkConnectionStatus == NetworkConnectionStatus.Connected)
            {
                await _voiceServerConnection.RemoveBot(_networkConnection.Callsign);

                await _atisHubConnection.DisconnectAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                    AtisLetter));
            }

            _voiceServerConnection?.Disconnect();

            // Dispose of the ATIS publish timer to stop further publishing.
            if (_publishAtisTimer != null)
            {
                await _publishAtisTimer.DisposeAsync();
            }

            _publishAtisTimer = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error disconnecting from voice server");
            NotificationManager?.Show(
                new Notification("Voice Server Error", ex.Message),
                type: NotificationType.Error,
                expiration: TimeSpan.Zero
            );
        }
    }

    private async Task HandleNetworkConnect()
    {
        try
        {
            if (_appConfig.ConfigRequired)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    if (lifetime.MainWindow == null)
                        return;

                    if (await MessageBox.ShowDialog(lifetime.MainWindow,
                            $"It looks like you haven't set your VATSIM user ID, password, and real name yet. Would you like to set them now?",
                            "Confirm", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                    {
                        EventBus.Instance.Publish(new OpenGenerateSettingsDialog());
                    }
                }

                return;
            }

            if (_networkConnection == null)
                return;

            if (_networkConnection.IsConnected)
            {
                await Disconnect();
                return;
            }

            try
            {
                if (_sessionManager.CurrentConnectionCount >= _sessionManager.MaxConnectionCount)
                {
                    NotificationManager?.Show(
                        new Notification("Too Many ATIS Connections",
                            "You've exceeded the maximum number of allowed ATIS connections."),
                        type: NotificationType.Warning,
                        expiration: TimeSpan.FromSeconds(15)
                    );
                    NativeAudio.EmitSound(SoundType.Error);
                    return;
                }

                NetworkConnectionStatus = NetworkConnectionStatus.Connecting;

                // Fetch the real-world ATIS letter if the user has enabled this option.
                if (_appConfig.AutoFetchAtisLetter && !string.IsNullOrEmpty(Identifier))
                {
                    var requestDto = new DigitalAtisRequestDto { Id = Identifier, AtisType = AtisType };
                    var atisLetter = await _atisHubConnection.GetDigitalAtisLetter(requestDto);
                    if (atisLetter != null)
                    {
                        await SetAtisLetterCommand.Execute(atisLetter.Value);
                    }
                }

                await _networkConnection.Connect();
            }
            catch (Exception e)
            {
                Log.Error(e, "HandleNetworkConnect Exception");
                NativeAudio.EmitSound(SoundType.Error);
                NotificationManager?.Show(new Notification("Error", e.Message), NotificationType.Error);
                await Disconnect();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleNetworkConnect Exception");
            NotificationManager?.Show(new Notification("Error", e.Message), NotificationType.Error);
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    private void OnNetworkConnectionFailed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
            Metar = null;
            Wind = null;
            ObservationTime = null;
            Altimeter = null;
        });
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkErrorReceived(object? sender, NetworkErrorReceived e)
    {
        NotificationManager?.Show(new Notification("Network Error", e.Error), NotificationType.Error);
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkConnected(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NetworkConnectionStatus = NetworkConnectionStatus.Connected;
            IsNewAtis = false; // Reset to avoid flashing ATIS letter

            PopulateAirportConditions(true);
            PopulateNotams(true);

            // Increase connection count by one
            _sessionManager.CurrentConnectionCount++;
        });
    }

    private void OnNetworkDisconnected(object? sender, NetworkDisconnectedReceived e)
    {
        _decodedMetar = null;

        Dispatcher.UIThread.Post(() =>
        {
            NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
            Metar = null;
            Wind = null;
            ObservationTime = null;
            Altimeter = null;
            IsNewAtis = false;

            // Decrease the current connection count by one, ensuring it doesn't go below zero
            _sessionManager.CurrentConnectionCount = Math.Max(_sessionManager.CurrentConnectionCount - 1, 0);

            if (e.CallsignInuse)
            {
                // Reset selected preset
                _previousAtisPreset = null;
                SelectedAtisPreset = null;

                // Subscribe to ATIS again if we were disconnected due to duplicate callsign
                SubscribeToAtis();
            }
        });
    }

    private void OnChangeServerReceived(object? sender, ClientEventArgs<string> e)
    {
        _ = Disconnect();
        _networkConnection?.Connect(e.Value);
    }

    private async void OnMetarResponseReceived(object? sender, MetarResponseReceived e)
    {
        try
        {
            await ProcessMetarResponseAsync(e);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnMetarResponseReceived Exception");
        }
    }

    private async Task ProcessMetarResponseAsync(MetarResponseReceived e)
    {
        try
        {
            if (_voiceServerConnection == null || _networkConnection == null)
                return;

            if (SelectedAtisPreset == null)
                return;

            if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
                return;

            // Cancel previous request
            await _processMetarCts.CancelAsync();
            _processMetarCts = new CancellationTokenSource();
            var localToken = _processMetarCts;

            if (e.IsNewMetar)
            {
                IsNewAtis = false;
                if (!_appConfig.MuteOwnAtisUpdateSound && IsVisibleOnMiniWindow)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }

                await AcknowledgeOrIncrementAtisLetterCommand.Execute();
                IsNewAtis = true;
                RecordedAtisState = RecordedAtisState.Expired;
            }

            // Save the decoded metar so its individual properties can be sent to clients
            // connected via the websocket.
            _decodedMetar = e.Metar;

            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Metar = e.Metar.RawMetar?.ToUpperInvariant();
                    Altimeter = e.Metar.Pressure?.Value?.ActualUnit == Value.Unit.HectoPascal
                        ? "Q" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000")
                        : "A" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000");
                    Wind = e.Metar.SurfaceWind?.RawValue;
                    ObservationTime = e.Metar.Time;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update METAR properties.");
                throw;
            }

            if (AtisStation.AtisVoice.UseTextToSpeech)
            {
                try
                {
                    // Prevent duplicate subscriber notifications.
                    // Send notification only when the initial METAR is received upon first connection.
                    await BuildAtis(localToken.Token, notifySubscribers: !e.IsNewMetar);
                }
                catch (OperationCanceledException)
                {
                    // Swallow cancellation, since it is expected behavior
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnMetarResponseReceived Exception");
                    NotificationManager?.Show(new Notification("Error", ex.Message), NotificationType.Error);
                    await Disconnect();
                    NativeAudio.EmitSound(SoundType.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnMetarResponseReceived Exception");
            NotificationManager?.Show(new Notification("Error", ex.Message), NotificationType.Error);
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    /// <summary>
    /// Publishes the current ATIS information to connected websocket clients.
    /// </summary>
    /// <param name="session">The connected client to publish the data to.
    /// If omitted or null, the data is broadcast to all connected clients.</param>
    /// <returns>A task.</returns>
    private async Task PublishAtisToWebsocket(ClientMetadata? session = null)
    {
        var airportConditions = await Dispatcher.UIThread.InvokeAsync(() => AirportConditionsTextDocument?.Text);
        var notams = await Dispatcher.UIThread.InvokeAsync(() => NotamsTextDocument?.Text);
        var ceilingLayer = AtisStation.AtisFormat.GetCeilingLayer(_decodedMetar?.Clouds);

        await _websocketService.SendAtisMessageAsync(session,
            new AtisMessage.AtisMessageValue
            {
                Id = AtisStation.Id,
                Station = AtisStation.Identifier,
                AtisType = AtisStation.AtisType,
                AtisLetter = AtisLetter,
                Metar = Metar?.Trim(),
                Wind = Wind?.Trim(),
                Altimeter = Altimeter?.Trim(),
                TextAtis = AtisStation.TextAtis,
                AirportConditions = airportConditions?.Trim(),
                Notams = notams?.Trim(),
                IsNewAtis = IsNewAtis,
                NetworkConnectionStatus = NetworkConnectionStatus,
                Pressure = _decodedMetar?.Pressure?.Value ?? null,
                Ceiling = ceilingLayer?.BaseHeight ?? null,
                PrevailingVisibility = _decodedMetar?.Visibility?.PrevailingVisibility ?? null
            });
    }

    private async Task PublishAtisToHub()
    {
        // Publish ATIS immediately
        await PublishAtis();

        // Dispose of the existing timer to start a new one.
        if (_publishAtisTimer != null)
        {
            await _publishAtisTimer.DisposeAsync();
        }

        _publishAtisTimer = null;

        // Set up a new timer to re-publish ATIS every 3 minutes.
        // The Timer callback uses an async void method to handle exceptions explicitly.
        _publishAtisTimer = new Timer(PublishAtisTimerCallback, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
    }

    private async void PublishAtisTimerCallback(object? state)
    {
        try
        {
            await PublishAtis();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish ATIS to hub");
        }
    }

    private async Task PublishAtis()
    {
        try
        {
            // Retrieve the values on the UI thread.
            var airportConditions = await Dispatcher.UIThread.InvokeAsync(() => AirportConditionsTextDocument?.Text);
            var notams = await Dispatcher.UIThread.InvokeAsync(() => NotamsTextDocument?.Text);

            await _atisHubConnection.PublishAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim(), airportConditions?.Trim(), notams?.Trim(),
                AtisStation.TextAtis?.Trim()));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish ATIS to hub");
        }
    }

    private async Task HandleSelectedAtisPresetChanged(AtisPreset? preset)
    {
        try
        {
            if (preset == null || _voiceServerConnection == null)
                return;

            await _selectedPresetCts.CancelAsync();
            _selectedPresetCts = new CancellationTokenSource();
            var localToken = _selectedPresetCts;

            if (preset != _previousAtisPreset)
            {
                if (HasUnsavedNotams || HasUnsavedAirportConditions)
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                    {
                        if (lifetime.MainWindow == null)
                            return;

                        if (await MessageBox.ShowDialog(lifetime.MainWindow,
                                "You have unsaved Airport Conditions or NOTAMs. Would you like to save them first?",
                                "Confirm", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                        {
                            ApplyAirportConditionsCommand.Execute(true).Subscribe();
                            ApplyNotamsCommand.Execute(true).Subscribe();
                        }
                    }
                }

                SelectedAtisPreset = preset;
                _previousAtisPreset = preset;

                if (NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                    return;

                PopulateAirportConditions(presetChanged: true);
                PopulateNotams(presetChanged: true);

                HasUnsavedNotams = false;
                HasUnsavedAirportConditions = false;

                if (NetworkConnectionStatus != NetworkConnectionStatus.Connected || _networkConnection == null)
                    return;

                if (_decodedMetar == null)
                    return;

                await BuildAtis(localToken.Token, notifySubscribers: false);
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation, since it is expected behavior
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleSelectedAtisPresetChanged Exception");
            NotificationManager?.Show(new Notification("Error", e.Message), NotificationType.Error);
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    private async Task RequestVoiceAtis(CancellationToken cancellationToken)
    {
        if (SelectedAtisPreset == null)
            return;

        if (_decodedMetar == null || _voiceServerConnection == null || _networkConnection == null)
            return;

        if (AtisStation.AtisVoice.UseTextToSpeech)
        {
            await Task.Run(async () =>
            {
                await _voiceRequestLock.WaitAsync(cancellationToken);
                try
                {
                    var voiceAtis = await _atisBuilder.BuildVoiceAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        _decodedMetar, cancellationToken);

                    if (voiceAtis.AudioBytes != null)
                    {
                        var dto = AtisBotUtils.CreateAtisBotDto(voiceAtis.AudioBytes, AtisStation.Frequency,
                            _atisStationAirport.Latitude, _atisStationAirport.Longitude, TransceiverHeightM);

                        await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task was canceled, ignore
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing voice ATIS request");
                }
                finally
                {
                    _voiceRequestLock.Release();
                }
            }, cancellationToken);
        }
    }

    private void PopulateNotams(bool presetChanged = false)
    {
        if (NotamsTextDocument == null || SelectedAtisPreset == null)
            return;

        // Retrieve and sort enabled NOTAMs by their ordinal value.
        var staticDefinitions = AtisStation.NotamDefinitions
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

        ReadOnlyNotams.Clear();
        NotamsTextDocument.Text = "";
        _notamFreeTextOffset = 0;

        var staticDefinitionsString = string.Join(". ", staticDefinitions.Select(s => s.Text.TrimEnd('.'))) + ". ";

        // Insert static definitions before free-text
        if (AtisStation.NotamsBeforeFreeText)
        {
            if (staticDefinitions.Count > 0)
            {
                // Insert static definitions
                NotamsTextDocument.Insert(0, staticDefinitionsString);

                // Mark static definitions segment as readonly
                ReadOnlyNotams.Add(new TextSegment { StartOffset = 0, Length = staticDefinitionsString.Length });

                _notamFreeTextOffset = staticDefinitionsString.Length;
            }

            // Insert free-text after static definitions
            if (!string.IsNullOrEmpty(_previousFreeTextNotams))
            {
                NotamsTextDocument.Insert(_notamFreeTextOffset, _previousFreeTextNotams.Trim());
            }
            else if (!string.IsNullOrEmpty(SelectedAtisPreset.Notams))
            {
                NotamsTextDocument.Insert(_notamFreeTextOffset, SelectedAtisPreset.Notams);
            }
        }
        else
        {
            var textToInsert = !string.IsNullOrEmpty(_previousFreeTextNotams)
                ? _previousFreeTextNotams.Trim()
                : SelectedAtisPreset?.Notams?.Trim();

            if (!string.IsNullOrEmpty(textToInsert))
            {
                var hasStaticDefinitions = staticDefinitions.Count > 0;
                NotamsTextDocument.Insert(0, hasStaticDefinitions ? textToInsert + " " : textToInsert);
                _notamFreeTextOffset = textToInsert.Length + (hasStaticDefinitions ? 1 : 0);
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
        if (AirportConditionsTextDocument == null || SelectedAtisPreset == null)
            return;

        // Retrieve and sort enabled static airport conditions by their ordinal value.
        var staticDefinitions = AtisStation.AirportConditionDefinitions
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

        ReadOnlyAirportConditions.Clear();
        AirportConditionsTextDocument.Text = "";
        _airportConditionsFreeTextOffset = 0;

        var staticDefinitionsString = string.Join(". ", staticDefinitions.Select(s => s.Text.TrimEnd('.'))) + ". ";

        // Insert static definitions before free-text
        if (AtisStation.AirportConditionsBeforeFreeText)
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
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset,
                    _previousFreeTextAirportConditions.Trim());
            }
            else if (!string.IsNullOrEmpty(SelectedAtisPreset.AirportConditions))
            {
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset,
                    SelectedAtisPreset.AirportConditions);
            }
        }
        else
        {
            var textToInsert = !string.IsNullOrEmpty(_previousFreeTextAirportConditions)
                ? _previousFreeTextAirportConditions.Trim()
                : SelectedAtisPreset?.AirportConditions?.Trim();

            if (!string.IsNullOrEmpty(textToInsert))
            {
                var hasStaticDefinitions = staticDefinitions.Count > 0;
                AirportConditionsTextDocument.Insert(0, hasStaticDefinitions ? textToInsert + " " : textToInsert);
                _airportConditionsFreeTextOffset = textToInsert.Length + (hasStaticDefinitions ? 1 : 0);
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

    private async void HandleIsNewAtisChanged(bool isNewAtis)
    {
        try
        {
            await PublishAtisToWebsocket();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in HandleIsNewAtisChanged");
        }
    }

    private async Task HandleAtisLetterChanged()
    {
        if (!AtisStation.AtisVoice.UseTextToSpeech ||
            NetworkConnectionStatus != NetworkConnectionStatus.Connected ||
            SelectedAtisPreset == null ||
            _networkConnection == null ||
            _voiceServerConnection == null ||
            _decodedMetar == null)
        {
            return;
        }

        await _atisLetterChangedCts.CancelAsync();
        _atisLetterChangedCts = new CancellationTokenSource();
        var localToken = _atisLetterChangedCts.Token;

        try
        {
            await BuildAtis(localToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HandleAtisLetterChanged Exception");
            NotificationManager?.Show(new Notification("Error", ex.Message), NotificationType.Error);
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
            });
        }
    }

    private async Task BuildAtis(CancellationToken cancelToken, bool notifySubscribers = true)
    {
        if (SelectedAtisPreset == null || _decodedMetar == null)
            return;

        if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
            return;

        try
        {
            // Only allow one request at a time
            await _buildAtisLock.WaitAsync(cancelToken);

            try
            {
                // Builds the textual ATIS
                var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                    _decodedMetar, cancelToken);

                // Sets the textual ATIS
                AtisStation.TextAtis = textAtis?.ToUpperInvariant();

                // Sets the ATIS letter
                AtisStation.AtisLetter = AtisLetter;

                // Publishes the ATIS to the hub server
                await PublishAtisToHub();

                // Publishes the ATIS to connected websocket clients
                await PublishAtisToWebsocket();

                if (notifySubscribers)
                {
                    // Notifies subscribed VATSIM clients
                    _networkConnection?.SendSubscriberNotification(AtisLetter);
                }

                // Posts an update to the configured IDS endpoint URL
                await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, cancelToken);

                // Requests a new voice ATIS
                await RequestVoiceAtis(cancelToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            finally
            {
                _buildAtisLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private void OnGetAtisReceived(object? sender, GetAtisReceived e)
    {
        // Throw exception to the websocket client if both ID and Station are provided.
        if (!string.IsNullOrEmpty(e.StationId) && !string.IsNullOrEmpty(e.Station))
        {
            throw new Exception("Cannot provide both Id and Station.");
        }

        // If a specific station ID is provided, it must match the current station's ID.
        if (!string.IsNullOrEmpty(e.StationId) && e.StationId != AtisStation.Id)
        {
            return;
        }

        // If a station identifier is provided, both the identifier and the ATIS type must match.
        if (!string.IsNullOrEmpty(e.Station) &&
            (e.Station != AtisStation.Identifier || e.AtisType != AtisStation.AtisType))
        {
            return;
        }

        // If no specific station ID or identifier is provided, the request is treated as a request for all stations.
        PublishAtisToWebsocket(e.Session).SafeFireAndForget();
    }

    private void OnAcknowledgeAtisUpdateReceived(object? sender, AcknowledgeAtisUpdateReceived e)
    {
        // Throw exception to the websocket client if both ID and Station are provided.
        if (!string.IsNullOrEmpty(e.StationId) && !string.IsNullOrEmpty(e.Station))
        {
            throw new Exception("Cannot provide both Id and Station.");
        }

        // If a specific station ID is provided, it must match the current station's ID.
        if (!string.IsNullOrEmpty(e.StationId) && e.StationId != AtisStation.Id)
        {
            return;
        }

        // If a station identifier is provided, both the identifier and the ATIS type must match.
        if (!string.IsNullOrEmpty(e.Station) &&
            (e.Station != AtisStation.Identifier || e.AtisType != AtisStation.AtisType))
        {
            return;
        }

        // If no specific station ID or identifier is provided, the request is treated as a request for all stations.
        HandleAcknowledgeAtisUpdate();
    }

    private void OnConfigureAtisReceived(object? sender, GetConfigureAtisReceived e)
    {
        if (e.Payload == null)
            return;

        if (!string.IsNullOrEmpty(e.Payload.Id) && !string.IsNullOrEmpty(e.Payload.Station))
            throw new Exception("Cannot provide both Id and Station.");

        var isMatchingId = !string.IsNullOrEmpty(e.Payload.Id) && AtisStation.Id == e.Payload.Id;
        var isMatchingStation = !string.IsNullOrEmpty(e.Payload.Station) &&
                                AtisStation.Identifier == e.Payload.Station &&
                                AtisStation.AtisType == e.Payload.AtisType;

        if (isMatchingId || isMatchingStation)
        {
            var preset = AtisStation.Presets.FirstOrDefault(x => x.Name == e.Payload.Preset);
            if (preset != null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    SelectedAtisPreset = preset;
                    UpdatePresetData(e.Payload);
                });
            }
            else
            {
                throw new Exception($"Invalid Preset: {e.Payload.Preset}");
            }
        }
    }

    private void UpdatePresetData(ConfigureAtisMessage.ConfigureAtisMessagePayload payload)
    {
        if (SelectedAtisPreset != null)
        {
            SelectedAtisPreset.AirportConditions = payload.AirportConditionsFreeText ?? "";
            SelectedAtisPreset.Notams = payload.NotamsFreeText ?? "";
        }

        if (AirportConditionsTextDocument != null)
        {
            AirportConditionsTextDocument.Text = payload.AirportConditionsFreeText ?? "";
        }

        if (NotamsTextDocument != null)
        {
            NotamsTextDocument.Text = payload.NotamsFreeText ?? "";
        }
    }

    private void OnConnectAtisReceived(object? sender, GetConnectAtisReceived e)
    {
        if (e.Payload == null)
            return;

        if (!string.IsNullOrEmpty(e.Payload.Id) && !string.IsNullOrEmpty(e.Payload.Station))
            throw new Exception("Cannot provide both Id and Station.");

        if (SelectedAtisPreset == null || NetworkConnectionStatus == NetworkConnectionStatus.Connected)
            return;

        var isMatchingId = !string.IsNullOrEmpty(e.Payload.Id) && AtisStation.Id == e.Payload.Id;
        var isMatchingStation = !string.IsNullOrEmpty(e.Payload.Station) &&
                                AtisStation.Identifier == e.Payload.Station &&
                                AtisStation.AtisType == e.Payload.AtisType;

        if (isMatchingId || isMatchingStation)
        {
            Dispatcher.UIThread.Invoke(() => { NetworkConnectCommand.Execute().Subscribe(); });
        }
    }

    private void OnDisconnectAtisReceived(object? sender, GetDisconnectAtisReceived e)
    {
        if (e.Payload == null)
            return;

        if (!string.IsNullOrEmpty(e.Payload.Id) && !string.IsNullOrEmpty(e.Payload.Station))
            throw new Exception("Cannot provide both Id and Station.");

        if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
            return;

        var isMatchingId = !string.IsNullOrEmpty(e.Payload.Id) && AtisStation.Id == e.Payload.Id;
        var isMatchingStation = !string.IsNullOrEmpty(e.Payload.Station) &&
                                AtisStation.Identifier == e.Payload.Station &&
                                AtisStation.AtisType == e.Payload.AtisType;

        if (isMatchingId || isMatchingStation)
        {
            Dispatcher.UIThread.Invoke(async () => { await Disconnect(); });
        }
    }

    private void HandleAcknowledgeAtisUpdate(PointerPressedEventArgs? args = null)
    {
        if (IsNewAtis)
        {
            if (args != null)
            {
                var point = args.GetCurrentPoint(null);
                if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
                {
                    EventBus.Instance.Publish(new AcknowledgeAllAtisUpdates());
                }
            }

            IsNewAtis = false;
        }
    }

    private void AcknowledgeOrIncrementAtisLetter()
    {
        if (IsNewAtis)
        {
            IsNewAtis = false;
            return;
        }

        if (AtisLetter >= AtisStation.CodeRange.High)
            AtisLetter = AtisStation.CodeRange.Low;
        else
            AtisLetter++;
    }

    private void DecrementAtisLetter()
    {
        if (AtisLetter <= AtisStation.CodeRange.Low)
            AtisLetter = AtisStation.CodeRange.High;
        else
            AtisLetter--;
    }
}
