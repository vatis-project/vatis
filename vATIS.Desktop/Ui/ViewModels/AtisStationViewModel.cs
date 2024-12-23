using ReactiveUI;
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
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Voice.Utils;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Ui.Services;
using System.Diagnostics;
using System.Text.Json;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;

namespace Vatsim.Vatis.Ui.ViewModels;
public class AtisStationViewModel : ReactiveViewModelBase
{
    private readonly IAppConfig mAppConfig;
    private readonly IAtisBuilder mAtisBuilder;
    private readonly AtisStation mAtisStation;
    private readonly IWindowFactory mWindowFactory;
    private readonly NetworkConnection? mNetworkConnection;
    private readonly IVoiceServerConnection? mVoiceServerConnection;
    private readonly IAtisHubConnection mAtisHubConnection;
    private readonly IWebsocketService mWebsocketService;

    private readonly ISessionManager mSessionManager;
    private CancellationTokenSource mCancellationToken;
    private readonly Airport mAtisStationAirport;
    private AtisPreset? mPreviousAtisPreset;
    private IDisposable? mPublishAtisTimer;
    private bool mIsPublishAtisTriggeredInitially;

    #region Reactive Properties
    private string? mId;
    public string? Id
    {
        get => mId;
        private set => this.RaiseAndSetIfChanged(ref mId, value);
    }

    private string? mIdentifier;
    public string? Identifier
    {
        get => mIdentifier;
        set => this.RaiseAndSetIfChanged(ref mIdentifier, value);
    }

    private string? mTabText;
    public string? TabText
    {
        get => mTabText;
        set => this.RaiseAndSetIfChanged(ref mTabText, value);
    }

    private char mAtisLetter = 'A';
    public char AtisLetter
    {
        get => mAtisLetter;
        set => this.RaiseAndSetIfChanged(ref mAtisLetter, value);
    }

    private bool mIsAtisLetterInputMode;
    public bool IsAtisLetterInputMode
    {
        get => mIsAtisLetterInputMode;
        set => this.RaiseAndSetIfChanged(ref mIsAtisLetterInputMode, value);
    }

    private string? mMetar;
    public string? Metar
    {
        get => mMetar;
        set => this.RaiseAndSetIfChanged(ref mMetar, value);
    }

    private string? mWind;
    public string? Wind
    {
        get => mWind;
        set => this.RaiseAndSetIfChanged(ref mWind, value);
    }

    private string? mAltimeter;
    public string? Altimeter
    {
        get => mAltimeter;
        set => this.RaiseAndSetIfChanged(ref mAltimeter, value);
    }

    private bool mIsNewAtis;
    public bool IsNewAtis
    {
        get => mIsNewAtis;
        set => this.RaiseAndSetIfChanged(ref mIsNewAtis, value);
    }

    private string mAtisTypeLabel = "";
    public string AtisTypeLabel
    {
        get => mAtisTypeLabel;
        set => this.RaiseAndSetIfChanged(ref mAtisTypeLabel, value);
    }

    private bool mIsCombinedAtis;
    public bool IsCombinedAtis
    {
        get => mIsCombinedAtis;
        private set => this.RaiseAndSetIfChanged(ref mIsCombinedAtis, value);
    }

    private ObservableCollection<AtisPreset> mAtisPresetList = [];
    public ObservableCollection<AtisPreset> AtisPresetList
    {
        get => mAtisPresetList;
        set => this.RaiseAndSetIfChanged(ref mAtisPresetList, value);
    }

    private AtisPreset? mSelectedAtisPreset;
    public AtisPreset? SelectedAtisPreset
    {
        get => mSelectedAtisPreset;
        private set => this.RaiseAndSetIfChanged(ref mSelectedAtisPreset, value);
    }

    private string? mErrorMessage;
    public string? ErrorMessage
    {
        get => mErrorMessage;
        set => this.RaiseAndSetIfChanged(ref mErrorMessage, value);
    }

    private string? AirportConditionsFreeText
    {
        get => AirportConditionsTextDocument?.Text;
        set => AirportConditionsTextDocument = new TextDocument(value);
    }

    private TextDocument? mAirportConditionsTextDocument = new();
    public TextDocument? AirportConditionsTextDocument
    {
        get => mAirportConditionsTextDocument;
        set => this.RaiseAndSetIfChanged(ref mAirportConditionsTextDocument, value);
    }

    private string? NotamsFreeText
    {
        get => mNotamsTextDocument?.Text;
        set => NotamsTextDocument = new TextDocument(value);
    }

    private TextDocument? mNotamsTextDocument = new();
    public TextDocument? NotamsTextDocument
    {
        get => mNotamsTextDocument;
        set => this.RaiseAndSetIfChanged(ref mNotamsTextDocument, value);
    }

    private bool mUseTexToSpeech;
    private bool UseTexToSpeech
    {
        get => mUseTexToSpeech;
        set => this.RaiseAndSetIfChanged(ref mUseTexToSpeech, value);
    }

    private NetworkConnectionStatus mNetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    public NetworkConnectionStatus NetworkConnectionStatus
    {
        get => mNetworkConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref mNetworkConnectionStatus, value);
    }

    private List<ICompletionData> mContractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => mContractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref mContractionCompletionData, value);
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

    public AtisStationViewModel(AtisStation station, INetworkConnectionFactory connectionFactory, IAppConfig appConfig,
        IVoiceServerConnection voiceServerConnection, IAtisBuilder atisBuilder, IWindowFactory windowFactory,
        INavDataRepository navDataRepository, IAtisHubConnection atisHubConnection, ISessionManager sessionManager, IWebsocketService websocketService)
    {
        Id = station.Id;
        Identifier = station.Identifier;
        mAtisStation = station;
        mAppConfig = appConfig;
        mAtisBuilder = atisBuilder;
        mWindowFactory = windowFactory;
        mAtisHubConnection = atisHubConnection;
        mWebsocketService = websocketService;
        mSessionManager = sessionManager;
        mCancellationToken = new CancellationTokenSource();
        mAtisStationAirport = navDataRepository.GetAirport(station.Identifier) ??
                              throw new ApplicationException($"{station.Identifier} not found in airport navdata.");

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

        OpenStaticAirportConditionsDialogCommand = ReactiveCommand.Create(HandleOpenAirportConditionsDialog);
        OpenStaticNotamsDialogCommand = ReactiveCommand.Create(HandleOpenStaticNotamsDialog);

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

        mWebsocketService.OnGetAtisReceived += async (session, station) =>
        {
            if (station != mAtisStation.Identifier)
            {
                return;
            }

            await PublishAtisToWebsocket(session);

            Debug.WriteLine($"Received GetAtis for {station}");
            Debug.WriteLine($"AtisStation connected status: {this.NetworkConnectionStatus}");
        };

        LoadContractionData();

        mNetworkConnection = connectionFactory.CreateConnection(mAtisStation);
        mNetworkConnection.NetworkConnectionFailed += OnNetworkConnectionFailed;
        mNetworkConnection.NetworkErrorReceived += OnNetworkErrorReceived;
        mNetworkConnection.NetworkConnected += OnNetworkConnected;
        mNetworkConnection.NetworkDisconnected += OnNetworkDisconnected;
        mNetworkConnection.ChangeServerReceived += OnChangeServerReceived;
        mNetworkConnection.MetarResponseReceived += OnMetarResponseReceived;
        mNetworkConnection.KillRequestReceived += OnKillRequestedReceived;
        mVoiceServerConnection = voiceServerConnection;

        UseTexToSpeech = !mAtisStation.AtisVoice.UseTextToSpeech;
        MessageBus.Current.Listen<AtisVoiceTypeChanged>().Subscribe(evt =>
        {
            if (evt.Id == mAtisStation.Id)
            {
                UseTexToSpeech = !evt.UseTextToSpeech;
            }
        });
        MessageBus.Current.Listen<StationPresetsChanged>().Subscribe(evt =>
        {
            if (evt.Id == mAtisStation.Id)
            {
                AtisPresetList = new ObservableCollection<AtisPreset>(mAtisStation.Presets.OrderBy(x => x.Ordinal));
            }
        });
        MessageBus.Current.Listen<ContractionsUpdated>().Subscribe(evt =>
        {
            if (evt.StationId == mAtisStation.Id)
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
                AtisLetter = sync.Dto.AtisLetter;
                Wind = sync.Dto.Wind;
                Altimeter = sync.Dto.Altimeter;
                Metar = sync.Dto.Metar;
                NetworkConnectionStatus = NetworkConnectionStatus.Observer;
            }
        });
        MessageBus.Current.Listen<AtisHubExpiredAtisReceived>().Subscribe(sync =>
        {
            if (sync.Dto.StationId == mAtisStation.Identifier &&
                sync.Dto.AtisType == mAtisStation.AtisType &&
                NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                AtisLetter = 'A';
                Wind = null;
                Altimeter = null;
                Metar = null;
                NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
            }
        });
        MessageBus.Current.Listen<HubConnected>().Subscribe(_ =>
        {
            mAtisHubConnection.SubscribeToAtis(new SubscribeDto(mAtisStation.Identifier, mAtisStation.AtisType));
        });

        this.WhenAnyValue(x => x.AtisLetter).Subscribe(HandleAtisLetterChanged);
        this.WhenAnyValue(x => x.NetworkConnectionStatus).Skip(1).Subscribe(HandleNetworkStatusChanged);
    }

    private void HandleSaveNotamsText()
    {
        if (SelectedAtisPreset == null)
            return;

        SelectedAtisPreset.Notams = NotamsFreeText;
        mAppConfig.SaveConfig();

        HasUnsavedNotams = false;
    }

    private void HandleSaveAirportConditionsText()
    {
        if (SelectedAtisPreset == null)
            return;

        SelectedAtisPreset.AirportConditions = AirportConditionsFreeText;
        mAppConfig.SaveConfig();

        HasUnsavedAirportConditions = false;
    }

    private void LoadContractionData()
    {
        ContractionCompletionData.Clear();

        foreach (var contraction in mAtisStation.Contractions.ToList())
        {
            if (contraction is { VariableName: not null, Voice: not null })
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
        }
    }

    private void HandleOpenStaticNotamsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        var dlg = mWindowFactory.CreateStaticNotamsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticNotamsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(mAtisStation.NotamDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                mAtisStation.NotamsBeforeFreeText = val;
                mAppConfig.SaveConfig();
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
            {
                mAtisStation.NotamDefinitions.Clear();
                mAtisStation.NotamDefinitions.AddRange(changes);
                mAppConfig.SaveConfig();
            });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                mAtisStation.NotamDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    mAtisStation.NotamDefinitions.Add(item);
                }
                mAppConfig.SaveConfig();
            };
        }

        dlg.ShowDialog(lifetime.MainWindow);
    }

    private void HandleOpenAirportConditionsDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        var dlg = mWindowFactory.CreateStaticAirportConditionsDialog();
        dlg.Topmost = lifetime.MainWindow.Topmost;
        if (dlg.DataContext is StaticAirportConditionsDialogViewModel viewModel)
        {
            viewModel.Definitions = new ObservableCollection<StaticDefinition>(mAtisStation.AirportConditionDefinitions);
            viewModel.ContractionCompletionData = ContractionCompletionData;

            viewModel.WhenAnyValue(x => x.IncludeBeforeFreeText).Subscribe(val =>
            {
                mAtisStation.AirportConditionsBeforeFreeText = val;
                mAppConfig.SaveConfig();
            });

            viewModel.Definitions.ToObservableChangeSet().AutoRefresh(x => x.Enabled).Bind(out var changes).Subscribe(_ =>
            {
                mAtisStation.AirportConditionDefinitions.Clear();
                mAtisStation.AirportConditionDefinitions.AddRange(changes);
                mAppConfig.SaveConfig();
            });

            viewModel.Definitions.CollectionChanged += (_, _) =>
            {
                var idx = 0;
                mAtisStation.AirportConditionDefinitions.Clear();
                foreach (var item in viewModel.Definitions)
                {
                    item.Ordinal = ++idx;
                    mAtisStation.AirportConditionDefinitions.Add(item);
                }
                mAppConfig.SaveConfig();
            };
        }

        dlg.ShowDialog(lifetime.MainWindow);
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

            if (mNetworkConnection == null || mVoiceServerConnection == null)
                return;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                    return;

                var window = mWindowFactory.CreateVoiceRecordAtisDialog();
                if (window.DataContext is VoiceRecordAtisDialogViewModel vm)
                {
                    var atisBuilder = await mAtisBuilder.BuildAtis(mAtisStation, SelectedAtisPreset, AtisLetter, null,
                        mCancellationToken.Token);

                    vm.AtisScript = atisBuilder.TextAtis;
                    window.Topmost = lifetime.MainWindow.Topmost;

                    if (await window.ShowDialog<bool>(lifetime.MainWindow))
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(vm.AudioBuffer, mAtisStation.Frequency,
                                mAtisStationAirport.Latitude, mAtisStationAirport.Longitude, 100);
                            await mVoiceServerConnection?.AddOrUpdateBot(mNetworkConnection.Callsign, dto,
                                mCancellationToken.Token)!;

                            mAtisStation.TextAtis = atisBuilder.TextAtis;

                            mNetworkConnection?.SendSubscriberNotification(AtisLetter);
                            await mAtisBuilder.UpdateIds(mAtisStation, SelectedAtisPreset, AtisLetter,
                                mCancellationToken.Token);
                        }).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                ErrorMessage = string.Join(",",
                                    t.Exception.InnerExceptions.Select(exception => exception.Message));
                                mNetworkConnection?.Disconnect();
                                NativeAudio.EmitSound(SoundType.Error);
                            }
                        }, mCancellationToken.Token);
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
            if (mVoiceServerConnection == null || mNetworkConnection == null)
                return;

            switch (status)
            {
                case NetworkConnectionStatus.Connected:
                    {
                        try
                        {
                            await mVoiceServerConnection.Connect(mAppConfig.UserId, mAppConfig.PasswordDecrypted);
                            mSessionManager.CurrentConnectionCount++;
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
                            mSessionManager.CurrentConnectionCount =
                                Math.Max(mSessionManager.CurrentConnectionCount - 1, 0);
                            await mVoiceServerConnection.RemoveBot(mNetworkConnection.Callsign);
                            mVoiceServerConnection?.Disconnect();
                            mPublishAtisTimer?.Dispose();
                            mPublishAtisTimer = null;
                            mIsPublishAtisTriggeredInitially = false;
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

            if (mAppConfig.ConfigRequired)
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

            if (mNetworkConnection == null)
                return;

            if (!mNetworkConnection.IsConnected)
            {
                try
                {
                    if (mSessionManager.CurrentConnectionCount >= mSessionManager.MaxConnectionCount)
                    {
                        ErrorMessage = "Maximum ATIS connections exceeded.";
                        NativeAudio.EmitSound(SoundType.Error);
                        return;
                    }

                    NetworkConnectionStatus = NetworkConnectionStatus.Connecting;
                    await mNetworkConnection.Connect();
                }
                catch (Exception e)
                {
                    NativeAudio.EmitSound(SoundType.Error);
                    ErrorMessage = e.Message;
                    mNetworkConnection?.Disconnect();
                    NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
                }
            }
            else
            {
                mNetworkConnection?.Disconnect();
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
        mCancellationToken.Cancel();
        mCancellationToken.Dispose();
        mCancellationToken = new CancellationTokenSource();

        Dispatcher.UIThread.Post(() =>
        {
            NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
            Metar = null;
            Wind = null;
            Altimeter = null;
        });
    }

    private void OnChangeServerReceived(object? sender, ClientEventArgs<string> e)
    {
        mNetworkConnection?.Disconnect();
        mNetworkConnection?.Connect(e.Value);
    }

    private async void OnMetarResponseReceived(object? sender, MetarResponseReceived e)
    {
        try
        {
            if (mVoiceServerConnection == null || mNetworkConnection == null)
                return;

            if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
                return;

            if (SelectedAtisPreset == null)
                return;

            if (e.IsNewMetar)
            {
                if (!mAppConfig.SuppressNotificationSound)
                {
                    NativeAudio.EmitSound(SoundType.Notification);
                }
                AcknowledgeOrIncrementAtisLetterCommand.Execute().Subscribe();
                IsNewAtis = true;
            }

            Dispatcher.UIThread.Post(() =>
            {
                Metar = e.Metar.RawMetar?.ToUpperInvariant();
                Altimeter = e.Metar.Pressure?.Value?.ActualUnit == Value.Unit.HectoPascal
                    ? "Q" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000")
                    : "A" + e.Metar.Pressure?.Value?.ActualValue.ToString("0000");
                Wind = e.Metar.SurfaceWind?.RawValue;
            });

            if (mAtisStation.AtisVoice.UseTextToSpeech)
            {
                try
                {
                    // Cancel previous request
                    await mCancellationToken.CancelAsync();
                    mCancellationToken.Dispose();
                    mCancellationToken = new CancellationTokenSource();

                    var atisBuilder = await mAtisBuilder.BuildAtis(mAtisStation, SelectedAtisPreset, AtisLetter, null,
                        mCancellationToken.Token);

                    mAtisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                    mNetworkConnection?.SendSubscriberNotification(AtisLetter);
                    await mAtisBuilder.UpdateIds(mAtisStation, SelectedAtisPreset, AtisLetter, mCancellationToken.Token);
                    await PublishAtisToHub();
                    await PublishAtisToWebsocket();

                    if (atisBuilder.AudioBytes != null && mNetworkConnection != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, mAtisStation.Frequency,
                                mAtisStationAirport.Latitude, mAtisStationAirport.Longitude, 100);
                            await mVoiceServerConnection?.AddOrUpdateBot(mNetworkConnection.Callsign, dto, mCancellationToken.Token)!;
                        }).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                ErrorMessage = string.Join(",",
                                    t.Exception.InnerExceptions.Select(exception => exception.Message));
                                mNetworkConnection?.Disconnect();
                                NativeAudio.EmitSound(SoundType.Error);
                            }
                        }, mCancellationToken.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    mNetworkConnection?.Disconnect();
                    NativeAudio.EmitSound(SoundType.Error);
                }
            }
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
    public async Task PublishAtisToWebsocket(WebSocketSession? session = null)
    {
        // Construct the AtisMessage object. The value winds up being the same as the AtisHubDto object with
        // an additional field for the network connection status of the station.
        var atis = new AtisMessage
        {
            Value = new AtisMessage.AtisMessageValue(mAtisStation.Identifier, mAtisStation.AtisType, AtisLetter, Metar?.Trim(),
                            Wind?.Trim(), Altimeter?.Trim(), NetworkConnectionStatus is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer)
        };

        if (session is not null)
        {
            await session.SendAsync(JsonSerializer.Serialize(atis));
        }
        else
        {
            await mWebsocketService.SendAsync(JsonSerializer.Serialize(atis));
        }
    }

    private async Task PublishAtisToHub()
    {
        await mAtisHubConnection.PublishAtis(new AtisHubDto(mAtisStation.Identifier, mAtisStation.AtisType,
            AtisLetter, Metar?.Trim(), Wind?.Trim(), Altimeter?.Trim()));

        if (!mIsPublishAtisTriggeredInitially)
        {
            mIsPublishAtisTriggeredInitially = true;

            // ReSharper disable once AsyncVoidLambda
            mPublishAtisTimer = Observable.Interval(TimeSpan.FromMinutes(3)).Subscribe(async _ =>
            {
                await mAtisHubConnection.PublishAtis(new AtisHubDto(mAtisStation.Identifier, mAtisStation.AtisType,
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

            if (preset != mPreviousAtisPreset)
            {
                SelectedAtisPreset = preset;
                mPreviousAtisPreset = preset;

                AirportConditionsFreeText = SelectedAtisPreset.AirportConditions ?? "";
                NotamsFreeText = SelectedAtisPreset.Notams ?? "";

                HasUnsavedNotams = false;
                HasUnsavedAirportConditions = false;

                if (NetworkConnectionStatus != NetworkConnectionStatus.Connected || mNetworkConnection == null)
                    return;

                var atisBuilder = await mAtisBuilder.BuildAtis(mAtisStation, SelectedAtisPreset, AtisLetter, null,
                    mCancellationToken.Token);

                await PublishAtisToHub();
                await PublishAtisToWebsocket();
                await mAtisBuilder.UpdateIds(mAtisStation, SelectedAtisPreset, AtisLetter, mCancellationToken.Token);

                if (mAtisStation.AtisVoice.UseTextToSpeech)
                {
                    // Cancel previous request
                    await mCancellationToken.CancelAsync();
                    mCancellationToken.Dispose();
                    mCancellationToken = new CancellationTokenSource();

                    if (atisBuilder.AudioBytes != null)
                    {
                        await Task.Run(async () =>
                        {
                            var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, mAtisStation.Frequency,
                                mAtisStationAirport.Latitude, mAtisStationAirport.Longitude, 100);
                            await mVoiceServerConnection?.AddOrUpdateBot(mNetworkConnection.Callsign, dto,
                                mCancellationToken.Token)!;
                        }, mCancellationToken.Token);
                    }
                }
                else
                {
                    mAtisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();
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

    private async void HandleAtisLetterChanged(char atisLetter)
    {
        try
        {
            if (!mAtisStation.AtisVoice.UseTextToSpeech)
                return;

            if (NetworkConnectionStatus != NetworkConnectionStatus.Connected)
                return;

            if (SelectedAtisPreset == null)
                return;

            if (mNetworkConnection == null || mVoiceServerConnection == null)
                return;

            // Cancel previous request
            await mCancellationToken.CancelAsync();
            mCancellationToken.Dispose();
            mCancellationToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                try
                {
                    var atisBuilder = await mAtisBuilder.BuildAtis(mAtisStation, SelectedAtisPreset, atisLetter,
                        null, mCancellationToken.Token);

                    mAtisStation.TextAtis = atisBuilder.TextAtis?.ToUpperInvariant();

                    mNetworkConnection?.SendSubscriberNotification(AtisLetter);
                    await mAtisBuilder.UpdateIds(mAtisStation, SelectedAtisPreset, AtisLetter,
                        mCancellationToken.Token);
                    await PublishAtisToHub();
                    await PublishAtisToWebsocket();

                    if (atisBuilder.AudioBytes != null && mNetworkConnection != null)
                    {
                        var dto = AtisBotUtils.AddBotRequest(atisBuilder.AudioBytes, mAtisStation.Frequency,
                            mAtisStationAirport.Latitude, mAtisStationAirport.Longitude, 100);
                        mVoiceServerConnection?.AddOrUpdateBot(mNetworkConnection.Callsign, dto,
                            mCancellationToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => { ErrorMessage = ex.Message; });
                }

            }, mCancellationToken.Token);
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
        if (AtisLetter > 'Z')
            AtisLetter = 'A';
    }

    private void DecrementAtisLetter()
    {
        AtisLetter--;
        if (AtisLetter < 'A')
            AtisLetter = 'Z';
    }

    public void Disconnect()
    {
        mNetworkConnection?.Disconnect();
        NetworkConnectionStatus = NetworkConnectionStatus.Disconnected;
    }
}