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
using WatsonWebsocket;

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
    private CancellationTokenSource _voiceRequestCts = new();
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
    private string? _identifier;
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
    private string? _errorMessage;
    private TextDocument? _airportConditionsTextDocument = new();
    private TextDocument? _notamsTextDocument = new();
    private bool _useTexToSpeech;
    private NetworkConnectionStatus _networkConnectionStatus = NetworkConnectionStatus.Disconnected;
    private List<ICompletionData> _contractionCompletionData = [];
    private bool _hasUnsavedAirportConditions;
    private bool _hasUnsavedNotams;
    private string? _previousFreeTextNotams;
    private string? _previousFreeTextAirportConditions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisStationViewModel"/> class.
    /// </summary>
    /// <param name="station">
    /// The ATIS station instance associated with this view model.
    /// </param>
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
    public AtisStationViewModel(AtisStation station, INetworkConnectionFactory connectionFactory,
        IVoiceServerConnectionFactory voiceServerConnectionFactory, IAppConfig appConfig, IAtisBuilder atisBuilder,
        IWindowFactory windowFactory, INavDataRepository navDataRepository, IAtisHubConnection hubConnection,
        ISessionManager sessionManager, IProfileRepository profileRepository, IWebsocketService websocketService)
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
        SaveAirportConditionsText = ReactiveCommand.Create(HandleSaveAirportConditionsText);
        SaveNotamsText = ReactiveCommand.Create(HandleSaveNotamsText);
        SelectedPresetChangedCommand = ReactiveCommand.CreateFromTask<AtisPreset>(HandleSelectedAtisPresetChanged);
        AcknowledgeAtisUpdateCommand = ReactiveCommand.Create(HandleAcknowledgeAtisUpdate);
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
                AtisPresetList = new ObservableCollection<AtisPreset>(AtisStation.Presets.OrderBy(x => x.Ordinal));
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
                if (NetworkConnectionStatus == NetworkConnectionStatus.Observer &&
                    (sync.Dto.AtisLetter != AtisLetter ||
                     !string.Equals(sync.Dto.Metar, Metar, StringComparison.OrdinalIgnoreCase)))
                {
                    IsNewAtis = true;
                }

                AtisLetter = sync.Dto.AtisLetter;
                Wind = sync.Dto.Wind;
                Altimeter = sync.Dto.Altimeter;
                Metar = sync.Dto.Metar;
                ObservationTime = _decodedMetar?.Time.Replace(":", "");
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
                    // If the ATIS is offline, then populate the airport conditions
                    // and notams for the selected preset.
                    if (SelectedAtisPreset != null)
                    {
                        PopulateAirportConditions();
                        PopulateNotams();
                    }

                    // Otherwise, just empty the textboxes.
                    // This makes it appear as the offline state.
                    // Only the ATIS letter is still synced for a short period.
                    else
                    {
                        if (AirportConditionsTextDocument != null)
                        {
                            AirportConditionsTextDocument.Text = "";
                        }

                        if (NotamsTextDocument != null)
                        {
                            NotamsTextDocument.Text = "";
                        }
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
        _disposables.Add(EventBus.Instance.Subscribe<HubConnected>(_ =>
        {
            _atisHubConnection.SubscribeToAtis(new SubscribeDto(AtisStation.Identifier, AtisStation.AtisType));
        }));
        _disposables.Add(EventBus.Instance.Subscribe<SessionEnded>(_ =>
        {
            if (NetworkConnectionStatus == NetworkConnectionStatus.Connected)
            {
                _atisHubConnection.DisconnectAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                    AtisLetter));
            }

            _voiceServerConnection.RemoveBot(_networkConnection.Callsign);
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
    /// Gets the command that sets the ATIS letter. Used with fetching real-world D-ATIS letter.
    /// </summary>
    public ReactiveCommand<char, Unit> SetAtisLetterCommand { get; }

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
    /// Gets or sets the identifier associated with the ATIS station.
    /// </summary>
    public string? Identifier
    {
        get => _identifier;
        set => this.RaiseAndSetIfChanged(ref _identifier, value);
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
    /// Gets or sets the error message associated with the current operation or state.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
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
    /// Gets or sets a value indicating whether text-to-speech functionality is enabled.
    /// </summary>
    public bool UseTexToSpeech
    {
        get => _useTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref _useTexToSpeech, value);
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
    /// Gets or sets the collection of contraction completion data utilized for auto-completion.
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
        SaveAirportConditionsText.Dispose();
        SaveNotamsText.Dispose();

        // Dispose cancellation tokens
        _processMetarCts.Dispose();
        _voiceRecordAtisCts.Dispose();
        _selectedPresetCts.Dispose();
        _atisLetterChangedCts.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleSetAtisLetter(char letter)
    {
        if (letter < AtisStation.CodeRange.Low || letter > AtisStation.CodeRange.High)
            return;
        AtisLetter = letter;
    }

    private void HandleSaveNotamsText()
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

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedNotams = false;
    }

    private void HandleSaveAirportConditionsText()
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

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedAirportConditions = false;
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

        // Update the free-form text area after the dialog is closed
        PopulateNotams();
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

        // Update the free-form text area after the dialog is closed
        PopulateAirportConditions();
    }

    private void OnKillRequestedReceived(object? sender, KillRequestReceived e)
    {
        NativeAudio.EmitSound(SoundType.Error);

        Dispatcher.UIThread.Post(() =>
        {
            Wind = null;
            Altimeter = null;
            Metar = null;
            ObservationTime = null;
            ErrorMessage = string.IsNullOrEmpty(e.Reason)
                ? "Forcefully disconnected from network."
                : $"Forcefully disconnected from network: {e.Reason}";
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
                        }
                        catch (OperationCanceledException)
                        {
                            // Swallow cancellation, since it is expected behavior
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error in voice ATIS update");
                            ErrorMessage = ex.Message;
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = e.Message;
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = e.Message;
            });
        }
    }

    private async Task ConnectToVoiceServer()
    {
        try
        {
            if (_voiceServerConnection != null)
            {
                await _voiceServerConnection.Connect(_appConfig.UserId, _appConfig.PasswordDecrypted);
                _sessionManager.CurrentConnectionCount++;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HandleConnectedStatus Exception");
            ErrorMessage = ex.Message;
        }
    }

    private async Task DisconnectFromVoiceServer()
    {
        if (_voiceServerConnection == null || _networkConnection == null)
            return;

        try
        {
            _sessionManager.CurrentConnectionCount = Math.Max(_sessionManager.CurrentConnectionCount - 1, 0);
            await _voiceServerConnection.RemoveBot(_networkConnection.Callsign);
            _voiceServerConnection?.Disconnect();

            if (NetworkConnectionStatus == NetworkConnectionStatus.Connected)
            {
                await _atisHubConnection.DisconnectAtis(new AtisHubDto(AtisStation.Identifier, AtisStation.AtisType,
                    AtisLetter));
            }

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
            ErrorMessage = ex.Message;
        }
    }

    private async Task HandleNetworkConnect()
    {
        try
        {
            ErrorMessage = null;

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
                    ErrorMessage = "Maximum ATIS connections exceeded.";
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
                ErrorMessage = e.Message;
                await Disconnect();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleNetworkConnect Exception");
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = e.Message;
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
        Dispatcher.UIThread.Post(() => { ErrorMessage = e.Error; });
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkConnected(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => NetworkConnectionStatus = NetworkConnectionStatus.Connected);
    }

    private void OnNetworkDisconnected(object? sender, EventArgs e)
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

            if (NetworkConnectionStatus is NetworkConnectionStatus.Observer or NetworkConnectionStatus.Disconnected)
                return;

            // Cancel previous request
            await _processMetarCts.CancelAsync();
            _processMetarCts = new CancellationTokenSource();
            var localToken = _processMetarCts;

            if (e.IsNewMetar)
            {
                IsNewAtis = false;
                if (!_appConfig.SuppressNotificationSound)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }

                await AcknowledgeOrIncrementAtisLetterCommand.Execute();
                IsNewAtis = true;
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
                    var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        e.Metar, localToken.Token);

                    AtisStation.TextAtis = textAtis?.ToUpperInvariant();
                    AtisStation.AtisLetter = AtisLetter;

                    await PublishAtisToWebsocket();
                    await PublishAtisToHub();
                    await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, localToken.Token);

                    if (!e.IsNewMetar)
                    {
                        // Prevent duplicate subscriber notifications.
                        // Send notification only when the initial METAR is received upon first connection.
                        _networkConnection?.SendSubscriberNotification(AtisLetter);
                    }

                    await RequestVoiceAtis();
                }
                catch (OperationCanceledException)
                {
                    // Swallow cancellation, since it is expected behavior
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnMetarResponseReceived Exception");
                    ErrorMessage = ex.Message;
                    await Disconnect();
                    NativeAudio.EmitSound(SoundType.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnMetarResponseReceived Exception");
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = ex.Message;
            });
        }
    }

    /// <summary>
    /// Publishes the current ATIS information to connected websocket clients.
    /// </summary>
    /// <param name="session">The connected client to publish the data to. If omitted or null the data is broadcast to all connected clients.</param>
    /// <returns>A task.</returns>
    private async Task PublishAtisToWebsocket(ClientMetadata? session = null)
    {
        await _websocketService.SendAtisMessageAsync(session,
            new AtisMessage.AtisMessageValue
            {
                Station = AtisStation.Identifier,
                AtisType = AtisStation.AtisType,
                AtisLetter = AtisLetter,
                Metar = Metar?.Trim(),
                Wind = Wind?.Trim(),
                Altimeter = Altimeter?.Trim(),
                TextAtis = AtisStation.TextAtis,
                IsNewAtis = IsNewAtis,
                NetworkConnectionStatus = NetworkConnectionStatus,
                Pressure = _decodedMetar?.Pressure?.Value ?? null,
                Ceiling = _decodedMetar?.Ceiling?.BaseHeight ?? null,
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
                AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim(), airportConditions?.Trim(), notams?.Trim()));
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
                            SaveNotamsText.Execute().Subscribe();
                            SaveAirportConditionsText.Execute().Subscribe();
                        }
                    }
                }

                SelectedAtisPreset = preset;
                _previousAtisPreset = preset;

                PopulateAirportConditions(presetChanged: true);
                PopulateNotams(presetChanged: true);

                HasUnsavedNotams = false;
                HasUnsavedAirportConditions = false;

                if (NetworkConnectionStatus != NetworkConnectionStatus.Connected || _networkConnection == null)
                    return;

                if (_decodedMetar == null)
                    return;

                var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                    _decodedMetar, localToken.Token);

                AtisStation.TextAtis = textAtis?.ToUpperInvariant();
                AtisStation.AtisLetter = AtisLetter;

                await PublishAtisToHub();

                await PublishAtisToWebsocket();

                await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, localToken.Token);

                await RequestVoiceAtis();
            }
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation, since it is expected behavior
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleSelectedAtisPresetChanged Exception");
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = e.Message;
            });
        }
    }

    private async Task RequestVoiceAtis()
    {
        if (SelectedAtisPreset == null)
            return;

        if (_decodedMetar == null || _voiceServerConnection == null || _networkConnection == null)
            return;

        if (AtisStation.AtisVoice.UseTextToSpeech)
        {
            // Cancel any currently running task
            await _voiceRequestCts.CancelAsync();
            _voiceRequestCts = new CancellationTokenSource();

            var localCts = _voiceRequestCts;

            await Task.Run(async () =>
            {
                await _voiceRequestLock.WaitAsync(localCts.Token);
                try
                {
                    var voiceAtis = await _atisBuilder.BuildVoiceAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        _decodedMetar, localCts.Token);

                    if (voiceAtis.AudioBytes != null)
                    {
                        var dto = AtisBotUtils.CreateAtisBotDto(voiceAtis.AudioBytes, AtisStation.Frequency,
                            _atisStationAirport.Latitude, _atisStationAirport.Longitude, TransceiverHeightM);

                        await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto, localCts.Token);
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
            }, localCts.Token);
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
            if (!string.IsNullOrEmpty(_previousFreeTextNotams))
            {
                NotamsTextDocument.Insert(0, _previousFreeTextNotams.Trim() + " ");
                _notamFreeTextOffset = _previousFreeTextNotams.Trim().Length + 1;
            }
            else if (!string.IsNullOrEmpty(SelectedAtisPreset.Notams))
            {
                NotamsTextDocument.Insert(0, SelectedAtisPreset.Notams.Trim() + " ");
                _notamFreeTextOffset = SelectedAtisPreset.Notams.Trim().Length + 1;
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
            if (!string.IsNullOrEmpty(_previousFreeTextAirportConditions))
            {
                AirportConditionsTextDocument.Insert(0, _previousFreeTextAirportConditions.Trim() + " ");
                _airportConditionsFreeTextOffset = _previousFreeTextAirportConditions.Trim().Length + 1;
            }
            else if (!string.IsNullOrEmpty(SelectedAtisPreset.AirportConditions))
            {
                AirportConditionsTextDocument.Insert(0, SelectedAtisPreset.AirportConditions.Trim() + " ");
                _airportConditionsFreeTextOffset = SelectedAtisPreset.AirportConditions.Trim().Length + 1;
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
        await _atisLetterChangedCts.CancelAsync();
        _atisLetterChangedCts = new CancellationTokenSource();
        var localToken = _atisLetterChangedCts.Token;

        if (!AtisStation.AtisVoice.UseTextToSpeech ||
            NetworkConnectionStatus != NetworkConnectionStatus.Connected ||
            SelectedAtisPreset == null ||
            _networkConnection == null ||
            _voiceServerConnection == null ||
            _decodedMetar == null)
        {
            return;
        }

        try
        {
            var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                _decodedMetar, localToken);

            AtisStation.TextAtis = textAtis?.ToUpperInvariant();
            AtisStation.AtisLetter = AtisLetter;

            await PublishAtisToHub();

            await PublishAtisToWebsocket();

            _networkConnection?.SendSubscriberNotification(AtisLetter);

            await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, localToken);

            await RequestVoiceAtis();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HandleAtisLetterChanged Exception");
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ObservationTime = null;
                ErrorMessage = ex.Message;
            });
        }
    }

    private async void OnGetAtisReceived(object? sender, GetAtisReceived e)
    {
        try
        {
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
            await PublishAtisToWebsocket(e.Session);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in OnGetAtisReceived");
        }
    }

    private void OnAcknowledgeAtisUpdateReceived(object? sender, AcknowledgeAtisUpdateReceived e)
    {
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
                throw new Exception($"Invalid Preset Name: {e.Payload.Preset}");
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

        var isMatchingId = !string.IsNullOrEmpty(e.Payload.Id) && AtisStation.Id == e.Payload.Id;
        var isMatchingStation = !string.IsNullOrEmpty(e.Payload.Station) &&
                                AtisStation.Identifier == e.Payload.Station &&
                                AtisStation.AtisType == e.Payload.AtisType;

        if (isMatchingId || isMatchingStation)
        {
            Dispatcher.UIThread.Invoke(async () => { await Disconnect(); });
        }
    }

    private void HandleAcknowledgeAtisUpdate()
    {
        if (IsNewAtis)
        {
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
