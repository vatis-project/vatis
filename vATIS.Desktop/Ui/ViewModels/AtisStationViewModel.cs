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

public class AtisStationViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;
    private readonly IAtisBuilder _atisBuilder;
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly AtisStation _atisStation;
    private readonly Airport _atisStationAirport;
    private readonly INetworkConnection? _networkConnection;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IVoiceServerConnection? _voiceServerConnection;
    private readonly IWebsocketService _websocketService;
    private readonly IWindowFactory _windowFactory;
    private int _airportConditionsFreeTextOffset;
    private CancellationTokenSource _cancellationToken;
    private DecodedMetar? _decodedMetar;
    private bool _isPublishAtisTriggeredInitially;
    private int _notamFreeTextOffset;
    private AtisPreset? _previousAtisPreset;
    private IDisposable? _publishAtisTimer;

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
        this._atisStation = station;
        this._appConfig = appConfig;
        this._atisBuilder = atisBuilder;
        this._windowFactory = windowFactory;
        this._websocketService = websocketService;
        this._atisHubConnection = hubConnection;
        this._sessionManager = sessionManager;
        this._profileRepository = profileRepository;
        this._cancellationToken = new CancellationTokenSource();
        this._atisStationAirport = navDataRepository.GetAirport(station.Identifier) ??
                                   throw new ApplicationException(
                                       $"{station.Identifier} not found in airport navdata.");

        this._atisLetter = this._atisStation.CodeRange.Low;

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
                this.AtisTypeLabel = "";
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

        this._websocketService.GetAtisReceived += this.OnGetAtisReceived;
        this._websocketService.AcknowledgeAtisUpdateReceived += this.OnAcknowledgeAtisUpdateReceived;

        this.LoadContractionData();

        this._networkConnection = connectionFactory.CreateConnection(this._atisStation);
        this._networkConnection.NetworkConnectionFailed += this.OnNetworkConnectionFailed;
        this._networkConnection.NetworkErrorReceived += this.OnNetworkErrorReceived;
        this._networkConnection.NetworkConnected += this.OnNetworkConnected;
        this._networkConnection.NetworkDisconnected += this.OnNetworkDisconnected;
        this._networkConnection.ChangeServerReceived += this.OnChangeServerReceived;
        this._networkConnection.MetarResponseReceived += this.OnMetarResponseReceived;
        this._networkConnection.KillRequestReceived += this.OnKillRequestedReceived;
        this._voiceServerConnection = voiceServerConnection;

        this.UseTexToSpeech = !this._atisStation.AtisVoice.UseTextToSpeech;
        MessageBus.Current.Listen<AtisVoiceTypeChanged>().Subscribe(
            evt =>
            {
                if (evt.Id == this._atisStation.Id)
                {
                    this.UseTexToSpeech = !evt.UseTextToSpeech;
                }
            });
        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(
            evt =>
            {
                if (evt.Id == this._atisStation.Id)
                {
                    this.AtisPresetList =
                        new ObservableCollection<AtisPreset>(this._atisStation.Presets.OrderBy(x => x.Ordinal));
                }
            });
        MessageBus.Current.Listen<ContractionsUpdated>().Subscribe(
            evt =>
            {
                if (evt.StationId == this._atisStation.Id)
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
                if (sync.Dto.StationId == this._atisStation.Identifier &&
                    sync.Dto.AtisType == this._atisStation.AtisType &&
                    this.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            this.AtisLetter = this._atisStation.CodeRange.Low;
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
                this._atisHubConnection.SubscribeToAtis(
                    new SubscribeDto(this._atisStation.Identifier, this._atisStation.AtisType));
            });

        this.WhenAnyValue(x => x.IsNewAtis).Subscribe(this.HandleIsNewAtisChanged);
        this.WhenAnyValue(x => x.AtisLetter).Subscribe(this.HandleAtisLetterChanged);
        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1).Subscribe(this.HandleNetworkStatusChanged);
    }

    public TextSegmentCollection<TextSegment> ReadOnlyAirportConditions { get; set; }

    public TextSegmentCollection<TextSegment> ReadOnlyNotams { get; set; }

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

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this._websocketService.GetAtisReceived -= this.OnGetAtisReceived;
        this._websocketService.AcknowledgeAtisUpdateReceived -= this.OnAcknowledgeAtisUpdateReceived;

        if (this._networkConnection != null)
        {
            this._networkConnection.NetworkConnectionFailed -= this.OnNetworkConnectionFailed;
            this._networkConnection.NetworkErrorReceived -= this.OnNetworkErrorReceived;
            this._networkConnection.NetworkConnected -= this.OnNetworkConnected;
            this._networkConnection.NetworkDisconnected -= this.OnNetworkDisconnected;
            this._networkConnection.ChangeServerReceived -= this.OnChangeServerReceived;
            this._networkConnection.MetarResponseReceived -= this.OnMetarResponseReceived;
            this._networkConnection.KillRequestReceived -= this.OnKillRequestedReceived;
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

        this.SelectedAtisPreset.Notams = this.NotamsFreeText?[this._notamFreeTextOffset..];
        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
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
            this.AirportConditionsFreeText?[this._airportConditionsFreeTextOffset..];
        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedAirportConditions = false;
    }

    private void LoadContractionData()
    {
        this.ContractionCompletionData.Clear();

        foreach (var contraction in this._atisStation.Contractions.ToList())
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

        var dlg = this._windowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(this._atisStation.NotamDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this._atisStation.NotamsBeforeFreeText = val;
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this._atisStation.NotamDefinitions.Clear();
                    this._atisStation.NotamDefinitions.AddRange(changes);
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this._atisStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this._atisStation.NotamDefinitions.Add(item);
                }

                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
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

        var dlg = this._windowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions =
                new ObservableCollection<StaticDefinition>(this._atisStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = this.ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(
                val =>
                {
                    this._atisStation.AirportConditionsBeforeFreeText = val;
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(
                _ =>
                {
                    this._atisStation.AirportConditionDefinitions.Clear();
                    this._atisStation.AirportConditionDefinitions.AddRange(changes);
                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
                    }
                });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                this._atisStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    this._atisStation.AirportConditionDefinitions.Add(item);
                }

                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
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

            if (this._networkConnection == null || this._voiceServerConnection == null)
            {
                return;
            }

            if (this._decodedMetar == null)
            {
                return;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                {
                    return;
                }

                var window = this._windowFactory.CreateVoiceRecordAtisDialog();
                if (window.DataContext is VoiceRecordAtisDialogViewModel vm)
                {
                    var atisBuilder = await this._atisBuilder.BuildAtis(
                        this._atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        this._decodedMetar,
                        this._cancellationToken.Token);

                    vm.AtisScript = atisBuilder.TextAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        await Task.Run(
                            async () =>
                            {
                                this._atisStation.TextAtis = atisBuilder.TextAtis;

                                await this.PublishAtisToHub();
                                this._networkConnection.SendSubscriberNotification(this.AtisLetter);
                                await this._atisBuilder.UpdateIds(
                                    this._atisStation,
                                    this.SelectedAtisPreset,
                                    this.AtisLetter,
                                    this._cancellationToken.Token);

                                var dto = AtisBotUtils.AddBotRequest(
                                    vm.AudioBuffer,
                                    this._atisStation.Frequency,
                                    this._atisStationAirport.Latitude,
                                    this._atisStationAirport.Longitude,
                                    100);
                                await this._voiceServerConnection?.AddOrUpdateBot(
                                    this._networkConnection.Callsign,
                                    dto,
                                    this._cancellationToken.Token)!;
                            }).ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    this.ErrorMessage = string.Join(
                                        ",",
                                        t.Exception.InnerExceptions.Select(exception => exception.Message));
                                    this._networkConnection?.Disconnect();
                                    NativeAudio.EmitSound(SoundType.Error);
                                }
                            },
                            this._cancellationToken.Token);
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
            if (this._voiceServerConnection == null || this._networkConnection == null)
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
                        await this._voiceServerConnection.Connect(
                            this._appConfig.UserId,
                            this._appConfig.PasswordDecrypted);
                        this._sessionManager.CurrentConnectionCount++;
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
                        this._sessionManager.CurrentConnectionCount =
                            Math.Max(this._sessionManager.CurrentConnectionCount - 1, 0);
                        await this._voiceServerConnection.RemoveBot(this._networkConnection.Callsign);
                        this._voiceServerConnection?.Disconnect();
                        this._publishAtisTimer?.Dispose();
                        this._publishAtisTimer = null;
                        this._isPublishAtisTriggeredInitially = false;
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

            if (this._appConfig.ConfigRequired)
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

            if (this._networkConnection == null)
            {
                return;
            }

            if (!this._networkConnection.IsConnected)
            {
                try
                {
                    if (this._sessionManager.CurrentConnectionCount >= this._sessionManager.MaxConnectionCount)
                    {
                        this.ErrorMessage = "Maximum ATIS connections exceeded.";
                        NativeAudio.EmitSound(SoundType.Error);
                        return;
                    }

                    this.NetworkConnectionStatus = NetworkConnectionStatus.Connecting;
                    await this._networkConnection.Connect();
                }
                catch (Exception e)
                {
                    NativeAudio.EmitSound(SoundType.Error);
                    this.ErrorMessage = e.Message;
                    this._networkConnection?.Disconnect();
                    this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                }
            }
            else
            {
                this._networkConnection?.Disconnect();
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
        this._cancellationToken.Cancel();
        this._cancellationToken.Dispose();
        this._cancellationToken = new CancellationTokenSource();

        this._decodedMetar = null;

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
        this._networkConnection?.Disconnect();
        this._networkConnection?.Connect(e.Value);
    }

    private async void OnMetarResponseReceived(object? sender, MetarResponseReceived e)
    {
        try
        {
            if (this._voiceServerConnection == null || this._networkConnection == null)
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
                if (!this._appConfig.SuppressNotificationSound)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }

                this.AcknowledgeOrIncrementAtisLetterCommand.Execute().Subscribe();
                this.IsNewAtis = true;
            }

            // Save the decoded metar so its individual properties can be sent to clients
            // connected via the websocket.
            this._decodedMetar = e.Metar;

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

            if (this._atisStation.AtisVoice.UseTextToSpeech)
            {
                try
                {
                    // Cancel previous request
                    await this._cancellationToken.CancelAsync();
                    this._cancellationToken.Dispose();
                    this._cancellationToken = new CancellationTokenSource();

                    var atisBuilder = await this._atisBuilder.BuildAtis(
                        this._atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        e.Metar,
                        this._cancellationToken.Token);

                    this._atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                    await this.PublishAtisToHub();
                    this._networkConnection?.SendSubscriberNotification(this.AtisLetter);
                    await this._atisBuilder.UpdateIds(
                        this._atisStation,
                        this.SelectedAtisPreset,
                        this.AtisLetter,
                        this._cancellationToken.Token);

                    if (atisBuilder.AudioBytes != null && this._networkConnection != null)
                    {
                        await Task.Run(
                            async () =>
                            {
                                var dto = AtisBotUtils.AddBotRequest(
                                    atisBuilder.AudioBytes,
                                    this._atisStation.Frequency,
                                    this._atisStationAirport.Latitude,
                                    this._atisStationAirport.Longitude,
                                    100);
                                await this._voiceServerConnection?.AddOrUpdateBot(
                                    this._networkConnection.Callsign,
                                    dto,
                                    this._cancellationToken.Token)!;
                            }).ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    this.ErrorMessage = string.Join(
                                        ",",
                                        t.Exception.InnerExceptions.Select(exception => exception.Message));
                                    this._networkConnection?.Disconnect();
                                    NativeAudio.EmitSound(SoundType.Error);
                                }
                            },
                            this._cancellationToken.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    this.ErrorMessage = ex.Message;
                    this._networkConnection?.Disconnect();
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
        await this._websocketService.SendAtisMessage(
            session,
            new AtisMessage.AtisMessageValue
            {
                Station = this._atisStation.Identifier,
                AtisType = this._atisStation.AtisType,
                AtisLetter = this.AtisLetter,
                Metar = this.Metar?.Trim(),
                Wind = this.Wind?.Trim(),
                Altimeter = this.Altimeter?.Trim(),
                TextAtis = this._atisStation.TextAtis,
                IsNewAtis = this.IsNewAtis,
                NetworkConnectionStatus = this.NetworkConnectionStatus,
                PressureUnit = this._decodedMetar?.Pressure?.Value?.ActualUnit,
                PressureValue = this._decodedMetar?.Pressure?.Value?.ActualValue
            });
    }

    private async Task PublishAtisToHub()
    {
        await this._atisHubConnection.PublishAtis(
            new AtisHubDto(
                this._atisStation.Identifier,
                this._atisStation.AtisType,
                this.AtisLetter,
                this.Metar?.Trim(),
                this.Wind?.Trim(),
                this.Altimeter?.Trim()));

        // Setup timer to re-publish ATIS every 3 minutes to keep it active in the hub cache
        if (!this._isPublishAtisTriggeredInitially)
        {
            this._isPublishAtisTriggeredInitially = true;

            // ReSharper disable once AsyncVoidLambda
            this._publishAtisTimer = Observable.Interval(TimeSpan.FromMinutes(3)).Subscribe(
                async _ =>
                {
                    await this._atisHubConnection.PublishAtis(
                        new AtisHubDto(
                            this._atisStation.Identifier,
                            this._atisStation.AtisType,
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

            if (preset != this._previousAtisPreset)
            {
                this.SelectedAtisPreset = preset;
                this._previousAtisPreset = preset;

                this.PopulateAirportConditions();
                this.PopulateNotams();

                this.HasUnsavedNotams = false;
                this.HasUnsavedAirportConditions = false;

                if (this.NetworkConnectionStatus != NetworkConnectionStatus.Connected ||
                    this._networkConnection == null)
                {
                    return;
                }

                if (this._decodedMetar == null)
                {
                    return;
                }

                var atisBuilder = await this._atisBuilder.BuildAtis(
                    this._atisStation,
                    this.SelectedAtisPreset,
                    this.AtisLetter,
                    this._decodedMetar,
                    this._cancellationToken.Token);

                this._atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                await this.PublishAtisToHub();
                await this.PublishAtisToWebsocket();
                await this._atisBuilder.UpdateIds(
                    this._atisStation,
                    this.SelectedAtisPreset,
                    this.AtisLetter,
                    this._cancellationToken.Token);

                if (this._atisStation.AtisVoice.UseTextToSpeech)
                {
                    // Cancel previous request
                    await this._cancellationToken.CancelAsync();
                    this._cancellationToken.Dispose();
                    this._cancellationToken = new CancellationTokenSource();

                    if (atisBuilder.AudioBytes != null)
                    {
                        await Task.Run(
                            async () =>
                            {
                                var dto = AtisBotUtils.AddBotRequest(
                                    atisBuilder.AudioBytes,
                                    this._atisStation.Frequency,
                                    this._atisStationAirport.Latitude,
                                    this._atisStationAirport.Longitude,
                                    100);
                                await this._voiceServerConnection?.AddOrUpdateBot(
                                    this._networkConnection.Callsign,
                                    dto,
                                    this._cancellationToken.Token)!;
                            },
                            this._cancellationToken.Token);
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
        var staticDefinitions = this._atisStation.NotamDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        this.NotamsTextDocument.Text = "";

        // Reset offset
        this._notamFreeTextOffset = 0;

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
                    EndOffset = staticDefinitionsString.Length
                });

            // Update the starting index for the next insertion.
            this._notamFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form NOTAM text after the static definitions (if any).
        if (!string.IsNullOrEmpty(this.SelectedAtisPreset?.Notams))
        {
            this.NotamsTextDocument.Insert(this._notamFreeTextOffset, this.SelectedAtisPreset?.Notams);
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
        var staticDefinitions = this._atisStation.AirportConditionDefinitions
            .Where(x => x.Enabled)
            .OrderBy(x => x.Ordinal)
            .ToList();

        // Start with an empty document.
        this.AirportConditionsTextDocument.Text = "";

        // Reset offset
        this._airportConditionsFreeTextOffset = 0;

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
                    EndOffset = staticDefinitionsString.Length
                });

            // Update the starting index for the next insertion.
            this._airportConditionsFreeTextOffset = staticDefinitionsString.Length;
        }

        // Always append the free-form airport conditions after the static definitions (if any).
        if (!string.IsNullOrEmpty(this.SelectedAtisPreset?.AirportConditions))
        {
            this.AirportConditionsTextDocument.Insert(
                this._airportConditionsFreeTextOffset,
                this.SelectedAtisPreset?.AirportConditions);
        }
    }

    private async void HandleIsNewAtisChanged(bool isNewAtis)
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

    private async void HandleAtisLetterChanged(char atisLetter)
    {
        try
        {
            // Always publish the latest information to the websocket, even if the station isn't
            // connected or doesn't support text to speech.
            await this.PublishAtisToWebsocket();

            if (!this._atisStation.AtisVoice.UseTextToSpeech)
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

            if (this._networkConnection == null || this._voiceServerConnection == null)
            {
                return;
            }

            if (this._decodedMetar == null)
            {
                return;
            }

            // Cancel previous request
            await this._cancellationToken.CancelAsync();
            this._cancellationToken.Dispose();
            this._cancellationToken = new CancellationTokenSource();

            await Task.Run(
                async () =>
                {
                    try
                    {
                        var atisBuilder = await this._atisBuilder.BuildAtis(
                            this._atisStation,
                            this.SelectedAtisPreset,
                            atisLetter,
                            this._decodedMetar,
                            this._cancellationToken.Token);

                        this._atisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                        await this.PublishAtisToHub();
                        this._networkConnection?.SendSubscriberNotification(this.AtisLetter);
                        await this._atisBuilder.UpdateIds(
                            this._atisStation,
                            this.SelectedAtisPreset,
                            this.AtisLetter,
                            this._cancellationToken.Token);

                        if (atisBuilder.AudioBytes != null && this._networkConnection != null)
                        {
                            var dto = AtisBotUtils.AddBotRequest(
                                atisBuilder.AudioBytes,
                                this._atisStation.Frequency,
                                this._atisStationAirport.Latitude,
                                this._atisStationAirport.Longitude,
                                100);
                            this._voiceServerConnection?.AddOrUpdateBot(
                                this._networkConnection.Callsign,
                                dto,
                                this._cancellationToken.Token);
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
                this._cancellationToken.Token);
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
                (e.Station != this._atisStation.Identifier || e.AtisType != this._atisStation.AtisType))
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
            (e.Station != this._atisStation.Identifier || e.AtisType != this._atisStation.AtisType))
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
        if (this.AtisLetter > this._atisStation.CodeRange.High)
        {
            this.AtisLetter = this._atisStation.CodeRange.Low;
        }
    }

    private void DecrementAtisLetter()
    {
        this.AtisLetter--;
        if (this.AtisLetter < this._atisStation.CodeRange.Low)
        {
            this.AtisLetter = this._atisStation.CodeRange.High;
        }
    }

    public void Disconnect()
    {
        this._networkConnection?.Disconnect();
        this.NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    }

    #region Reactive Properties

    private string? _id;

    public string? Id
    {
        get => this._id;
        private set => this.RaiseAndSetIfChanged(ref this._id, value);
    }

    private string? _identifier;

    public string? Identifier
    {
        get => this._identifier;
        set => this.RaiseAndSetIfChanged(ref this._identifier, value);
    }

    private string? _tabText;

    public string? TabText
    {
        get => this._tabText;
        set => this.RaiseAndSetIfChanged(ref this._tabText, value);
    }

    private char _atisLetter;

    public char AtisLetter
    {
        get => this._atisLetter;
        set => this.RaiseAndSetIfChanged(ref this._atisLetter, value);
    }

    public CodeRangeMeta CodeRange => this._atisStation.CodeRange;

    private bool _isAtisLetterInputMode;

    public bool IsAtisLetterInputMode
    {
        get => this._isAtisLetterInputMode;
        set => this.RaiseAndSetIfChanged(ref this._isAtisLetterInputMode, value);
    }

    private string? _metar;

    public string? Metar
    {
        get => this._metar;
        set => this.RaiseAndSetIfChanged(ref this._metar, value);
    }

    private string? _wind;

    public string? Wind
    {
        get => this._wind;
        set => this.RaiseAndSetIfChanged(ref this._wind, value);
    }

    private string? _altimeter;

    public string? Altimeter
    {
        get => this._altimeter;
        set => this.RaiseAndSetIfChanged(ref this._altimeter, value);
    }

    private bool _isNewAtis;

    public bool IsNewAtis
    {
        get => this._isNewAtis;
        set => this.RaiseAndSetIfChanged(ref this._isNewAtis, value);
    }

    private string _atisTypeLabel = "";

    public string AtisTypeLabel
    {
        get => this._atisTypeLabel;
        set => this.RaiseAndSetIfChanged(ref this._atisTypeLabel, value);
    }

    private bool _isCombinedAtis;

    public bool IsCombinedAtis
    {
        get => this._isCombinedAtis;
        private set => this.RaiseAndSetIfChanged(ref this._isCombinedAtis, value);
    }

    private ObservableCollection<AtisPreset> _atisPresetList = [];

    public ObservableCollection<AtisPreset> AtisPresetList
    {
        get => this._atisPresetList;
        set => this.RaiseAndSetIfChanged(ref this._atisPresetList, value);
    }

    private AtisPreset? _selectedAtisPreset;

    public AtisPreset? SelectedAtisPreset
    {
        get => this._selectedAtisPreset;
        private set => this.RaiseAndSetIfChanged(ref this._selectedAtisPreset, value);
    }

    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => this._errorMessage;
        set => this.RaiseAndSetIfChanged(ref this._errorMessage, value);
    }

    private string? AirportConditionsFreeText => this.AirportConditionsTextDocument?.Text;

    private TextDocument? _airportConditionsTextDocument = new();

    public TextDocument? AirportConditionsTextDocument
    {
        get => this._airportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this._airportConditionsTextDocument, value);
    }

    private string? NotamsFreeText => this._notamsTextDocument?.Text;

    private TextDocument? _notamsTextDocument = new();

    public TextDocument? NotamsTextDocument
    {
        get => this._notamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref this._notamsTextDocument, value);
    }

    private bool _useTexToSpeech;

    private bool UseTexToSpeech
    {
        get => this._useTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref this._useTexToSpeech, value);
    }

    private NetworkConnectionStatus _networkConnectionStatus = NetworkConnectionStatus.Disconnected;

    public NetworkConnectionStatus NetworkConnectionStatus
    {
        get => this._networkConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref this._networkConnectionStatus, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];

    public List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
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

    #endregion
}