﻿using ReactiveUI;
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
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Voice.Utils;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.ViewModels;
public class AtisStationViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;
    private readonly IProfileRepository _profileRepository;
    private readonly IAtisBuilder _atisBuilder;
    private readonly AtisStation _atisStation;
    private readonly IWindowFactory _windowFactory;
    private readonly INetworkConnection? _networkConnection;
    private readonly IVoiceServerConnection? _voiceServerConnection;
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly IWebsocketService _websocketService;
    private readonly ISessionManager _sessionManager;
    private CancellationTokenSource _cancellationToken;
    private readonly Airport _atisStationAirport;
    private AtisPreset? _previousAtisPreset;
    private IDisposable? _publishAtisTimer;
    private bool _isPublishAtisTriggeredInitially;
    private DecodedMetar? _decodedMetar;
    private int _notamFreeTextOffset;
    private int _airportConditionsFreeTextOffset;

    public TextSegmentCollection<TextSegment> ReadOnlyAirportConditions { get; set; }
    public TextSegmentCollection<TextSegment> ReadOnlyNotams { get; set; }

    #region Reactive Properties
    private string? _id;
    public string? Id
    {
        get => _id;
        private set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private string? _identifier;
    public string? Identifier
    {
        get => _identifier;
        set => this.RaiseAndSetIfChanged(ref _identifier, value);
    }

    private string? _tabText;
    public string? TabText
    {
        get => _tabText;
        set => this.RaiseAndSetIfChanged(ref _tabText, value);
    }

    private char _atisLetter;
    public char AtisLetter
    {
        get => _atisLetter;
        set => this.RaiseAndSetIfChanged(ref _atisLetter, value);
    }

    public CodeRangeMeta CodeRange => _atisStation.CodeRange;

    private bool _isAtisLetterInputMode;
    public bool IsAtisLetterInputMode
    {
        get => _isAtisLetterInputMode;
        set => this.RaiseAndSetIfChanged(ref _isAtisLetterInputMode, value);
    }

    private string? _metar;
    public string? Metar
    {
        get => _metar;
        set => this.RaiseAndSetIfChanged(ref _metar, value);
    }

    private string? _wind;
    public string? Wind
    {
        get => _wind;
        set => this.RaiseAndSetIfChanged(ref _wind, value);
    }

    private string? _altimeter;
    public string? Altimeter
    {
        get => _altimeter;
        set => this.RaiseAndSetIfChanged(ref _altimeter, value);
    }

    private bool _isNewAtis;
    public bool IsNewAtis
    {
        get => _isNewAtis;
        set => this.RaiseAndSetIfChanged(ref _isNewAtis, value);
    }

    private string _atisTypeLabel = "";
    public string AtisTypeLabel
    {
        get => _atisTypeLabel;
        set => this.RaiseAndSetIfChanged(ref _atisTypeLabel, value);
    }

    private bool _isCombinedAtis;
    public bool IsCombinedAtis
    {
        get => _isCombinedAtis;
        private set => this.RaiseAndSetIfChanged(ref _isCombinedAtis, value);
    }

    private ObservableCollection<AtisPreset> _atisPresetList = [];
    public ObservableCollection<AtisPreset> AtisPresetList
    {
        get => _atisPresetList;
        set => this.RaiseAndSetIfChanged(ref _atisPresetList, value);
    }

    private AtisPreset? _selectedAtisPreset;
    public AtisPreset? SelectedAtisPreset
    {
        get => _selectedAtisPreset;
        private set => this.RaiseAndSetIfChanged(ref _selectedAtisPreset, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private string? AirportConditionsFreeText => AirportConditionsTextDocument?.Text;

    private TextDocument? _airportConditionsTextDocument = new();
    public TextDocument? AirportConditionsTextDocument
    {
        get => _airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _airportConditionsTextDocument, value);
    }

    private string? NotamsFreeText => _notamsTextDocument?.Text;

    private TextDocument? _notamsTextDocument = new();
    public TextDocument? NotamsTextDocument
    {
        get => _notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref _notamsTextDocument, value);
    }

    private bool _useTexToSpeech;
    private bool UseTexToSpeech
    {
        get => _useTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref _useTexToSpeech, value);
    }

    private NetworkConnectionStatus _networkConnectionStatus = NetworkConnectionStatus.Disconnected;
    public NetworkConnectionStatus NetworkConnectionStatus
    {
        get => _networkConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref _networkConnectionStatus, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
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

    public AtisType AtisType => _atisStation.AtisType;
    #endregion

    public ReactiveCommand<Unit, Unit> DecrementAtisLetterCommand { get; }
    public ReactiveCommand<Unit, Unit> AcknowledgeOrIncrementAtisLetterCommand { get; }
    public ReactiveCommand<Unit, Unit> AcknowledgeAtisUpdateCommand { get; }
    public ReactiveCommand<Unit, Unit> NetworkConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> VoiceRecordAtisCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenStaticAirportConditionsDialogCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenStaticNotamsDialogCommand { get; }
    public ReactiveCommand<AtisPreset, Unit> SelectedPresetChangedCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAirportConditionsText { get; }
    public ReactiveCommand<Unit, Unit> SaveNotamsText { get; }
    public ReactiveCommand<char, Unit> SetAtisLetterCommand { get; }

    public AtisStationViewModel(AtisStation station, INetworkConnectionFactory connectionFactory, IAppConfig appConfig,
        IVoiceServerConnection voiceServerConnection, IAtisBuilder atisBuilder, IWindowFactory windowFactory,
        INavDataRepository navDataRepository, IAtisHubConnection hubConnection, ISessionManager sessionManager,
        IProfileRepository profileRepository, IWebsocketService websocketService)
    {
        Id = station.Id;
        Identifier = station.Identifier;
        _atisStation = station;
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

        _atisLetter = _atisStation.CodeRange.Low;

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

        _networkConnection = connectionFactory.CreateConnection(_atisStation);
        _networkConnection.NetworkConnectionFailed += OnNetworkConnectionFailed;
        _networkConnection.NetworkErrorReceived += OnNetworkErrorReceived;
        _networkConnection.NetworkConnected += OnNetworkConnected;
        _networkConnection.NetworkDisconnected += OnNetworkDisconnected;
        _networkConnection.ChangeServerReceived += OnChangeServerReceived;
        _networkConnection.MetarResponseReceived += OnMetarResponseReceived;
        _networkConnection.KillRequestReceived += OnKillRequestedReceived;
        _voiceServerConnection = voiceServerConnection;

        UseTexToSpeech = !_atisStation.AtisVoice.UseTextToSpeech;
        MessageBus.Current.Listen<AtisVoiceTypeChanged>().Subscribe(evt =>
        {
            if (evt.Id == _atisStation.Id)
            {
                UseTexToSpeech = !evt.UseTextToSpeech;
            }
        });
        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(evt =>
        {
            if (evt.Id == _atisStation.Id)
            {
                AtisPresetList = new ObservableCollection<AtisPreset>(_atisStation.Presets.OrderBy(x => x.Ordinal));
            }
        });
        MessageBus.Current.Listen<ContractionsUpdated>().Subscribe(evt =>
        {
            if (evt.StationId == _atisStation.Id)
            {
                LoadContractionData();
            }
        });
        MessageBus.Current.Listen<AtisHubAtisReceived>().Subscribe(sync =>
        {
            if (sync.Dto.StationId == station.Identifier &&
                sync.Dto.AtisType == station.AtisType &&
                NetworkConnectionStatus != NetworkConnectionStatus.Connected)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AtisLetter = sync.Dto.AtisLetter;
                    Wind = sync.Dto.Wind;
                    Altimeter = sync.Dto.Altimeter;
                    Metar = sync.Dto.Metar;
                    NetworkConnectionStatus = NetworkConnectionStatus.Observer;
                });
            }
        });
        MessageBus.Current.Listen<AtisHubExpiredAtisReceived>().Subscribe(sync =>
        {
            if (sync.Dto.StationId == _atisStation.Identifier &&
                sync.Dto.AtisType == _atisStation.AtisType &&
                NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AtisLetter = _atisStation.CodeRange.Low;
                    Wind = null;
                    Altimeter = null;
                    Metar = null;
                    NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                });
            }
        });
        MessageBus.Current.Listen<HubConnected>().Subscribe(_ =>
        {
            _atisHubConnection.SubscribeToAtis(new SubscribeDto(_atisStation.Identifier, _atisStation.AtisType));
        });

        this.WhenAnyValue(x => x.IsNewAtis).Subscribe(HandleIsNewAtisChanged);
        this.WhenAnyValue(x => x.AtisLetter).Subscribe(HandleAtisLetterChanged);
        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1).Subscribe(HandleNetworkStatusChanged);
    }

    private void HandleSetAtisLetter(char letter)
    {
        if (letter < _atisStation.CodeRange.Low || letter > _atisStation.CodeRange.High)
            return;
        AtisLetter = letter;
    }

    private void HandleSaveNotamsText()
    {
        if (SelectedAtisPreset == null)
            return;

        SelectedAtisPreset.Notams = NotamsFreeText?[_notamFreeTextOffset..];
        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedNotams = false;
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedAtisPreset == null)
            return;

        SelectedAtisPreset.AirportConditions = AirportConditionsFreeText?[_airportConditionsFreeTextOffset..];
        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedAirportConditions = false;
    }

    private void LoadContractionData()
    {
        ContractionCompletionData.Clear();

        foreach (var contraction in _atisStation.Contractions.ToList())
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
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(_atisStation.NotamDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                _atisStation.NotamsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
            {
                _atisStation.NotamDefinitions.Clear();
                _atisStation.NotamDefinitions.AddRange(changes);
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                _atisStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    _atisStation.NotamDefinitions.Add(item);
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
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(_atisStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                _atisStation.AirportConditionsBeforeFreeText = val;
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
            {
                _atisStation.AirportConditionDefinitions.Clear();
                _atisStation.AirportConditionDefinitions.AddRange(changes);
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
            });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                _atisStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    _atisStation.AirportConditionDefinitions.Add(item);
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
            ErrorMessage = string.IsNullOrEmpty(e.Reason)
                ? $"Forcefully disconnected from network."
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
                    var atisBuilder = await _atisBuilder.BuildAtis(_atisStation, SelectedAtisPreset, AtisLetter, _decodedMetar,
                        _cancellationToken.Token);

                    vm.AtisScript = atisBuilder.TextAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        await Task.Run(async () =>
                        {
                            _atisStation.TextAtis = atisBuilder.TextAtis;

                            await PublishAtisToHub();
                            _networkConnection.SendSubscriberNotification(AtisLetter);
                            await _atisBuilder.UpdateIds(_atisStation, SelectedAtisPreset, AtisLetter,
                                _cancellationToken.Token);

                            var dto = AtisBotUtils.AddBotRequest(vm.AudioBuffer, _atisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection?.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                _cancellationToken.Token)!;
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
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
                            _publishAtisTimer?.Dispose();
                            _publishAtisTimer = null;
                            _isPublishAtisTriggeredInitially = false;
                        }
                        catch (Exception ex)
                        {
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
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
                        MessageBus.Current.SendMessage(new OpenGenerateSettingsDialog());
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
                    await _networkConnection.Connect();
                }
                catch (Exception e)
                {
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
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
            Altimeter = null;
        });
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkErrorReceived(object? sender, NetworkErrorReceived e)
    {
        ErrorMessage = e.Error;
        NativeAudio.EmitSound(SoundType.Error);
    }

    private void OnNetworkConnected(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => NetworkConnectionStatus = NetworkConnectionStatus.Connected);
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

            if (NetworkConnectionStatus == NetworkConnectionStatus.Disconnected ||
                NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

            if (SelectedAtisPreset == null)
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
                Metar = e.Metar.RawMetar?.ToUpperInvariant();
                Altimeter = e.Metar.Pressure?.Value?.ActualUnit == Value.Unit.HectoPascal
                    ? "Q" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000")
                    : "A" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000");
                Wind = e.Metar.SurfaceWind?.RawValue;
                propertyUpdates.SetResult();
            });

            // Wait for the UI thread to finish updating the properties. Without this it's possible
            // to publish updated METAR information either via the hub or websocket with old data.
            await propertyUpdates.Task;

            if (_atisStation.AtisVoice.UseTextToSpeech)
            {
                try
                {
                    // Cancel previous request
                    await _cancellationToken.CancelAsync();
                    _cancellationToken.Dispose();
                    _cancellationToken = new CancellationTokenSource();

                    var atisBuilder = await _atisBuilder.BuildAtis(_atisStation, SelectedAtisPreset, AtisLetter, e.Metar,
                        _cancellationToken.Token);

                    _atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                    await PublishAtisToHub();
                    _networkConnection?.SendSubscriberNotification(AtisLetter);
                    await _atisBuilder.UpdateIds(_atisStation, SelectedAtisPreset, AtisLetter, _cancellationToken.Token);

                    if (atisBuilder.AudioBytes != null && _networkConnection != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, _atisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection?.AddOrUpdateBot(_networkConnection.Callsign, dto, _cancellationToken.Token)!;
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
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    _networkConnection?.Disconnect();
                    NativeAudio.EmitSound(SoundType.Error);
                }
            }

            // This is done at the very end to ensure the TextAtis is updated before the websocket message is sent.
            await PublishAtisToWebsocket();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
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
            Station = _atisStation.Identifier,
            AtisType = _atisStation.AtisType,
            AtisLetter = AtisLetter,
            Metar = Metar?.Trim(),
            Wind = Wind?.Trim(),
            Altimeter = Altimeter?.Trim(),
            TextAtis = _atisStation.TextAtis,
            IsNewAtis = IsNewAtis,
            NetworkConnectionStatus = NetworkConnectionStatus,
            PressureUnit = _decodedMetar?.Pressure?.Value?.ActualUnit,
            PressureValue = _decodedMetar?.Pressure?.Value?.ActualValue,
        });
    }

    private async Task PublishAtisToHub()
    {
        await _atisHubConnection.PublishAtis(new AtisHubDto(_atisStation.Identifier, _atisStation.AtisType,
            AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim()));

        // Setup timer to re-publish ATIS every 3 minutes to keep it active in the hub cache
        if (!_isPublishAtisTriggeredInitially)
        {
            _isPublishAtisTriggeredInitially = true;

            // ReSharper disable once AsyncVoidLambda
            _publishAtisTimer = Observable.Interval(TimeSpan.FromMinutes(3)).Subscribe(async _ =>
            {
                await _atisHubConnection.PublishAtis(new AtisHubDto(_atisStation.Identifier, _atisStation.AtisType,
                    AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim()));
            });
        }
    }

    private async Task HandleSelectedAtisPresetChanged(AtisPreset? preset)
    {
        try
        {
            if (preset == null)
                return;

            if (preset != _previousAtisPreset)
            {
                SelectedAtisPreset = preset;
                _previousAtisPreset = preset;

                PopulateAirportConditions();
                PopulateNotams();

                HasUnsavedNotams = false;
                HasUnsavedAirportConditions = false;

                if (NetworkConnectionStatus != NetworkConnectionStatus.Connected || _networkConnection == null)
                    return;

                if (_decodedMetar == null)
                    return;

                var atisBuilder = await _atisBuilder.BuildAtis(_atisStation, SelectedAtisPreset, AtisLetter, _decodedMetar,
                    _cancellationToken.Token);

                _atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                await PublishAtisToHub();
                await PublishAtisToWebsocket();
                await _atisBuilder.UpdateIds(_atisStation, SelectedAtisPreset, AtisLetter, _cancellationToken.Token);

                if (_atisStation.AtisVoice.UseTextToSpeech)
                {
                    // Cancel previous request
                    await _cancellationToken.CancelAsync();
                    _cancellationToken.Dispose();
                    _cancellationToken = new CancellationTokenSource();

                    if (atisBuilder.AudioBytes != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, _atisStation.Frequency,
                                _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                            await _voiceServerConnection?.AddOrUpdateBot(_networkConnection.Callsign, dto,
                                _cancellationToken.Token)!;
                        }, _cancellationToken.Token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
                ErrorMessage = e.Message;
            });
        }
    }

    private void PopulateNotams()
    {
        if (NotamsTextDocument == null)
            return;

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyNotams.Clear();

        // Retrieve and sort enabled static NOTAM definitions by their ordinal value.
        var staticDefinitions = _atisStation.NotamDefinitions
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
        if (!string.IsNullOrEmpty(SelectedAtisPreset?.Notams))
        {
            NotamsTextDocument.Insert(_notamFreeTextOffset, SelectedAtisPreset?.Notams);
        }
    }

    private void PopulateAirportConditions()
    {
        if (AirportConditionsTextDocument == null)
            return;

        // Clear the list of read-only NOTAM text segments.
        ReadOnlyAirportConditions.Clear();

        // Retrieve and sort enabled static airport conditions by their ordinal value.
        var staticDefinitions = _atisStation.AirportConditionDefinitions
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
        if (!string.IsNullOrEmpty(SelectedAtisPreset?.AirportConditions))
        {
            AirportConditionsTextDocument.Insert(_airportConditionsFreeTextOffset,
                SelectedAtisPreset?.AirportConditions);
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

            if (!_atisStation.AtisVoice.UseTextToSpeech)
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
                    var atisBuilder = await _atisBuilder.BuildAtis(_atisStation, SelectedAtisPreset, atisLetter,
                        _decodedMetar, _cancellationToken.Token);

                    _atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                    await PublishAtisToHub();
                    _networkConnection?.SendSubscriberNotification(AtisLetter);
                    await _atisBuilder.UpdateIds(_atisStation, SelectedAtisPreset, AtisLetter,
                        _cancellationToken.Token);

                    if (atisBuilder.AudioBytes != null && _networkConnection != null)
                    {
                        var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, _atisStation.Frequency,
                            _atisStationAirport.Latitude, _atisStationAirport.Longitude, 100);
                        _voiceServerConnection?.AddOrUpdateBot(_networkConnection.Callsign, dto,
                            _cancellationToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
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
            Dispatcher.UIThread.Post(() =>
            {
                Wind = null;
                Altimeter = null;
                Metar = null;
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
                (e.Station != _atisStation.Identifier || e.AtisType != _atisStation.AtisType))
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
            (e.Station != _atisStation.Identifier || e.AtisType != _atisStation.AtisType))
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
        if (AtisLetter > _atisStation.CodeRange.High)
            AtisLetter = _atisStation.CodeRange.Low;
    }

    private void DecrementAtisLetter()
    {
        AtisLetter--;
        if (AtisLetter < _atisStation.CodeRange.Low)
            AtisLetter = _atisStation.CodeRange.High;
    }

    public void Disconnect()
    {
        _networkConnection?.Disconnect();
        NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

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
}
