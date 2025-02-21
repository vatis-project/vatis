﻿// <copyright file="AtisStationViewModel.cs" company="Justin Shannon">
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
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;
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
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a ViewModel for managing ATIS station information and operations.
/// </summary>
public class AtisStationViewModel : ReactiveViewModelBase, IDisposable
{
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
    private CancellationTokenSource _cancellationToken;
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
        _cancellationToken = new CancellationTokenSource();
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
        NetworkConnectCommand = ReactiveCommand.Create(HandleNetworkConnect, this.WhenAnyValue(
            x => x.SelectedAtisPreset,
            x => x.NetworkConnectionStatus,
            (atisPreset, networkStatus) => atisPreset != null && networkStatus != NetworkConnectionStatus.Connecting));
        VoiceRecordAtisCommand = ReactiveCommand.Create(HandleVoiceRecordAtisCommand,
            this.WhenAnyValue(
                x => x.Metar,
                x => x.UseTexToSpeech,
                x => x.NetworkConnectionStatus,
                (metar, voiceRecord, networkStatus) => !string.IsNullOrEmpty(metar) && voiceRecord &&
                                                       networkStatus == NetworkConnectionStatus.Connected));

        _websocketService.GetAtisReceived += OnGetAtisReceived;
        _websocketService.AcknowledgeAtisUpdateReceived += OnAcknowledgeAtisUpdateReceived;

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
            if (sync.Dto.StationId == station.Identifier &&
                sync.Dto.AtisType == station.AtisType &&
                NetworkConnectionStatus != NetworkConnectionStatus.Connected)
            {
                if (!string.IsNullOrEmpty(sync.Dto.Metar))
                {
                    _decodedMetar = _metarDecoder.ParseNotStrict(sync.Dto.Metar);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    AtisLetter = sync.Dto.AtisLetter;
                    Wind = sync.Dto.Wind;
                    Altimeter = sync.Dto.Altimeter;
                    Metar = sync.Dto.Metar;
                    ObservationTime = _decodedMetar?.Time.Replace(":", "");
                    NetworkConnectionStatus = NetworkConnectionStatus.Observer;

                    if (AirportConditionsTextDocument != null)
                    {
                        AirportConditionsTextDocument.Text = sync.Dto.AirportConditions ?? "";
                    }

                    if (NotamsTextDocument != null)
                    {
                        NotamsTextDocument.Text = sync.Dto.Notams ?? "";
                    }
                });
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisHubExpiredAtisReceived>(sync =>
        {
            if (sync.Dto.StationId == AtisStation.Identifier &&
                sync.Dto.AtisType == AtisStation.AtisType &&
                NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AtisLetter = AtisStation.CodeRange.Low;
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
            _voiceServerConnection.RemoveBot(_networkConnection.Callsign);
            _voiceServerConnection.Disconnect();
            _networkConnection.Disconnect();
        }));

        this.WhenAnyValue(x => x.IsNewAtis).Subscribe(HandleIsNewAtisChanged);
        this.WhenAnyValue(x => x.AtisLetter).Subscribe(HandleAtisLetterChanged);
        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1).Subscribe(HandleNetworkStatusChanged);
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
    public void Disconnect()
    {
        _networkConnection?.Disconnect();
        NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        _websocketService.GetAtisReceived -= OnGetAtisReceived;
        _websocketService.AcknowledgeAtisUpdateReceived -= OnAcknowledgeAtisUpdateReceived;

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

        var freeText = "";

        var readonlySegment = ReadOnlyNotams.FirstSegment;
        if (readonlySegment != null)
        {
            freeText = readonlySegment.StartOffset > 0
                ? NotamsTextDocument?.Text[..readonlySegment.StartOffset]
                : NotamsTextDocument?.Text[readonlySegment.Length..];
        }

        SelectedAtisPreset.Notams = string.IsNullOrEmpty(freeText) ? NotamsTextDocument?.Text.Trim() : freeText.Trim();

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedNotams = false;
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedAtisPreset == null)
            return;

        var freeText = "";

        var readonlySegment = ReadOnlyAirportConditions.FirstSegment;
        if (readonlySegment != null)
        {
            freeText = readonlySegment.StartOffset > 0
                ? AirportConditionsTextDocument?.Text[..readonlySegment.StartOffset]
                : AirportConditionsTextDocument?.Text[readonlySegment.Length..];
        }

        SelectedAtisPreset.AirportConditions =
            string.IsNullOrEmpty(freeText) ? AirportConditionsTextDocument?.Text.Trim() : freeText.Trim();

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

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
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

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
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

    private async void HandleVoiceRecordAtisCommand()
    {
        try
        {
            if (SelectedAtisPreset == null)
                return;

            if (_networkConnection == null || _voiceServerConnection == null)
                return;

            if (_decodedMetar == null)
                return;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                    return;

                var window = _windowFactory.CreateVoiceRecordAtisDialog();
                if (window.DataContext is VoiceRecordAtisDialogViewModel vm)
                {
                    var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter, _decodedMetar,
                        _cancellationToken.Token);

                    vm.AtisScript = textAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        await Task.Run(async () =>
                        {
                            AtisStation.TextAtis = textAtis;
                            AtisStation.AtisLetter = AtisLetter;

                            await PublishAtisToHub();
                            _networkConnection.SendSubscriberNotification(AtisLetter);
                            await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter,
                                _cancellationToken.Token);

                            var dto = AtisBotUtils.AddBotRequest(vm.AudioBuffer, AtisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                _cancellationToken.Token);
                        }).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                ErrorMessage = string.Join(",",
                                    t.Exception.InnerExceptions.Select(exception => exception.Message));
                                _networkConnection?.Disconnect();
                                NativeAudio.EmitSound(SoundType.Error);
                            }
                        }, _cancellationToken.Token);
                    }
                }
            }
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

    private async void HandleNetworkStatusChanged(NetworkConnectionStatus status)
    {
        try
        {
            if (_voiceServerConnection == null || _networkConnection == null)
                return;

            await PublishAtisToWebsocket();

            switch (status)
            {
                case NetworkConnectionStatus.Connected:
                    {
                        try
                        {
                            await _voiceServerConnection.Connect(_appConfig.UserId, _appConfig.PasswordDecrypted);
                            _sessionManager.CurrentConnectionCount++;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error connecting to voice server");
                            ErrorMessage = ex.Message;
                        }

                        break;
                    }

                case NetworkConnectionStatus.Disconnected:
                    {
                        try
                        {
                            _sessionManager.CurrentConnectionCount =
                                Math.Max(_sessionManager.CurrentConnectionCount - 1, 0);
                            await _voiceServerConnection.RemoveBot(_networkConnection.Callsign);
                            _voiceServerConnection?.Disconnect();

                            // Dispose of the ATIS publish timer to stop further publishing.
                            _publishAtisTimer?.Dispose();
                            _publishAtisTimer = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error disconnecting from voice server");
                            ErrorMessage = ex.Message;
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

    private async void HandleNetworkConnect()
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

            if (!_networkConnection.IsConnected)
            {
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
                    if (_appConfig.AutoFetchAtisLetter)
                    {
                        if (!string.IsNullOrEmpty(Identifier))
                        {
                            var requestDto = new DigitalAtisRequestDto { Id = Identifier, AtisType = AtisType };
                            var atisLetter = await _atisHubConnection.GetDigitalAtisLetter(requestDto);
                            if (atisLetter != null)
                            {
                                await SetAtisLetterCommand.Execute(atisLetter.Value);
                            }
                        }
                    }

                    await _networkConnection.Connect();
                }
                catch (Exception e)
                {
                    Log.Error(e, "HandleNetworkConnect Exception");
                    NativeAudio.EmitSound(SoundType.Error);
                    ErrorMessage = e.Message;
                    _networkConnection?.Disconnect();
                    NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                }
            }
            else
            {
                _networkConnection?.Disconnect();
                NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
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
        Dispatcher.UIThread.Post(() =>
        {
            ErrorMessage = e.Error;
        });
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkConnected(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NetworkConnectionStatus = NetworkConnectionStatus.Connected;
        });
    }

    private void OnNetworkDisconnected(object? sender, EventArgs e)
    {
        _cancellationToken.Cancel();
        _cancellationToken.Dispose();
        _cancellationToken = new CancellationTokenSource();

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
        _networkConnection?.Disconnect();
        _networkConnection?.Connect(e.Value);
    }

    private async void OnMetarResponseReceived(object? sender, MetarResponseReceived e)
    {
        try
        {
            if (_voiceServerConnection == null || _networkConnection == null)
                return;

            if (SelectedAtisPreset == null)
                return;

            if (NetworkConnectionStatus is NetworkConnectionStatus.Observer or NetworkConnectionStatus.Disconnected)
                return;

            if (e.IsNewMetar)
            {
                IsNewAtis = false;
                if (!_appConfig.SuppressNotificationSound)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }

                AcknowledgeOrIncrementAtisLetterCommand.Execute().Subscribe();
                IsNewAtis = true;
            }

            // Save the decoded metar so its individual properties can be sent to clients
            // connected via the websocket.
            _decodedMetar = e.Metar;

            var propertyUpdates = new TaskCompletionSource();
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    Metar = e.Metar.RawMetar?.ToUpperInvariant();
                    Altimeter = e.Metar.Pressure?.Value?.ActualUnit == Value.Unit.HectoPascal
                        ? "Q" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000")
                        : "A" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000");
                    Wind = e.Metar.SurfaceWind?.RawValue;
                    ObservationTime = e.Metar.Time;
                    propertyUpdates.SetResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update METAR properties.");
                    propertyUpdates.SetException(ex);
                }
            });

            // Wait for the UI thread to finish updating the properties. Without this it's possible
            // to publish updated METAR information either via the hub or websocket with old data.
            try
            {
                await propertyUpdates.Task;
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
                    // Cancel previous request
                    await _cancellationToken.CancelAsync();
                    _cancellationToken.Dispose();
                    _cancellationToken = new CancellationTokenSource();

                    var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter, e.Metar,
                        _cancellationToken.Token);

                    AtisStation.TextAtis = textAtis?.ToUpperInvariant();
                    AtisStation.AtisLetter = AtisLetter;

                    await PublishAtisToWebsocket();
                    await PublishAtisToHub();

                    if (!e.IsNewMetar)
                    {
                        // Prevent duplicate subscriber notifications.
                        // Send notification only when the initial METAR is received upon first connection.
                        _networkConnection?.SendSubscriberNotification(AtisLetter);
                    }

                    await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, _cancellationToken.Token);

                    var voiceAtis = await _atisBuilder.BuildVoiceAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        e.Metar, _cancellationToken.Token);

                    if (voiceAtis.AudioBytes != null && _networkConnection != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(voiceAtis.AudioBytes, AtisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                _cancellationToken.Token);
                        }).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                var errors = t.Exception.InnerExceptions
                                    .Select(ex => $"{ex.GetType().Name}: {ex.Message}").ToList();
                                ErrorMessage = string.Join(Environment.NewLine, errors);
                                Log.Error(t.Exception, "Failed to update voice ATIS");

                                _networkConnection?.Disconnect();
                                NativeAudio.EmitSound(SoundType.Error);
                            }
                        }, _cancellationToken.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnMetarResponseReceived Exception");
                    ErrorMessage = ex.Message;
                    _networkConnection?.Disconnect();
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
        await _websocketService.SendAtisMessage(session, new AtisMessage.AtisMessageValue
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
        _publishAtisTimer?.Dispose();
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
                AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim(), airportConditions, notams));
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
            if (preset == null)
                return;

            if (_voiceServerConnection == null)
                return;

            if (preset != _previousAtisPreset)
            {
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

                var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, AtisLetter, _decodedMetar,
                    _cancellationToken.Token);

                AtisStation.TextAtis = textAtis?.ToUpperInvariant();
                AtisStation.AtisLetter = AtisLetter;

                await PublishAtisToHub();
                await PublishAtisToWebsocket();
                await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter, _cancellationToken.Token);

                if (AtisStation.AtisVoice.UseTextToSpeech)
                {
                    // Cancel previous request
                    await _cancellationToken.CancelAsync();
                    _cancellationToken.Dispose();
                    _cancellationToken = new CancellationTokenSource();

                    var voiceAtis = await _atisBuilder.BuildVoiceAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        _decodedMetar, _cancellationToken.Token);

                    if (voiceAtis.AudioBytes != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(voiceAtis.AudioBytes, AtisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                _cancellationToken.Token);
                        }, _cancellationToken.Token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored
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
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset, _previousFreeTextAirportConditions.Trim());
            }
            else if (!string.IsNullOrEmpty(SelectedAtisPreset.AirportConditions))
            {
                AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset, SelectedAtisPreset.AirportConditions);
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

    private async void HandleAtisLetterChanged(char atisLetter)
    {
        try
        {
            // Always publish the latest information to the websocket, even if the station isn't
            // connected or doesn't support text to speech.
            await PublishAtisToWebsocket();

            if (!AtisStation.AtisVoice.UseTextToSpeech)
                return;

            if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
                return;

            if (SelectedAtisPreset == null)
                return;

            if (_networkConnection == null || _voiceServerConnection == null)
                return;

            if (_decodedMetar == null)
                return;

            // Cancel previous request
            await _cancellationToken.CancelAsync();
            _cancellationToken.Dispose();
            _cancellationToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                try
                {
                    var textAtis = await _atisBuilder.BuildTextAtis(AtisStation, SelectedAtisPreset, atisLetter,
                        _decodedMetar, _cancellationToken.Token);

                    AtisStation.TextAtis = textAtis?.ToUpperInvariant();
                    AtisStation.AtisLetter = AtisLetter;

                    await PublishAtisToHub();
                    _networkConnection?.SendSubscriberNotification(AtisLetter);
                    await _atisBuilder.UpdateIds(AtisStation, SelectedAtisPreset, AtisLetter,
                        _cancellationToken.Token);

                    var voiceAtis = await _atisBuilder.BuildVoiceAtis(AtisStation, SelectedAtisPreset, AtisLetter,
                        _decodedMetar, _cancellationToken.Token);

                    if (voiceAtis.AudioBytes != null && _networkConnection != null)
                    {
                        var dto = AtisBotUtils.AddBotRequest(voiceAtis.AudioBytes, AtisStation.Frequency,
                            _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                        await _voiceServerConnection.AddOrUpdateBot(_networkConnection.Callsign, dto,
                            _cancellationToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "HandleAtisLetterChanged Exception");
                    Dispatcher.UIThread.Post(() => { ErrorMessage = ex.Message; });
                }
            }, _cancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            Log.Error(e, "HandleAtisLetterChanged Exception");
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

    private async void OnGetAtisReceived(object? sender, GetAtisReceived e)
    {
        try
        {
            // If a specific station is specified then both the station identifier and the ATIS type
            // must match to acknowledge the update.
            // If a specific station isn't specified then the request is for all stations.
            if (!string.IsNullOrEmpty(e.Station) &&
                (e.Station != AtisStation.Identifier || e.AtisType != AtisStation.AtisType))
            {
                return;
            }

            await PublishAtisToWebsocket(e.Session);
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
            (e.Station != AtisStation.Identifier || e.AtisType != AtisStation.AtisType))
        {
            return;
        }

        HandleAcknowledgeAtisUpdate();
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

        AtisLetter++;
        if (AtisLetter > AtisStation.CodeRange.High)
            AtisLetter = AtisStation.CodeRange.Low;
    }

    private void DecrementAtisLetter()
    {
        AtisLetter--;
        if (AtisLetter < AtisStation.CodeRange.Low)
            AtisLetter = AtisStation.CodeRange.High;
    }
}
