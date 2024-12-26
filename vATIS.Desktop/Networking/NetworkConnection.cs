using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Vatsim.Network;
using Vatsim.Network.PDU;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Networking;

public class NetworkConnection : INetworkConnection
{
    private const string VATSIM_SERVER_ENDPOINT = "http://fsd.vatsim.net";
    private const string CLIENT_NAME = "vATIS";
    private const ushort CLIENT_ID = 0x579f;

    private readonly AtisStation? mAtisStation;
    private readonly FsdSession mFsdSession;
    private readonly ClientProperties mClientProperties;

    private readonly IAppConfig mAppConfig;
    private readonly IAuthTokenManager mAuthTokenManager;
    private readonly IMetarRepository mMetarRepository;
    private readonly IDownloader mDownloader;
    private readonly INavDataRepository mNavDataRepository;

    private readonly System.Timers.Timer mFsdPositionUpdateTimer;
    private readonly string mUniqueDeviceIdentifier;
    private string? mPublicIp;
    private string? mPreviousMetar;
    private readonly List<string> mSubscribers = [];
    private readonly List<string> mEuroscopeSubscribers = [];
    private readonly List<string> mClientCapabilitiesReceived = [];
    private readonly Airport? mAirportData;
    private readonly int mFsdFrequency;
    private DecodedMetar? mDecodedMetar;

    public event EventHandler NetworkConnected = delegate { };
    public event EventHandler NetworkDisconnected = delegate { };
    public event EventHandler NetworkConnectionFailed = delegate { };
    public event EventHandler<MetarResponseReceived> MetarResponseReceived = delegate { };
    public event EventHandler<NetworkErrorReceived> NetworkErrorReceived = delegate { };
    public event EventHandler<KillRequestReceived> KillRequestReceived = delegate { };
    public event EventHandler<ClientEventArgs<string>> ChangeServerReceived = delegate { };

    public string Callsign { get; }
    public bool IsConnected => mFsdSession.Connected;

    public NetworkConnection(AtisStation station, IAppConfig appConfig, IAuthTokenManager authTokenManager,
        IMetarRepository metarRepository, IDownloader downloader, INavDataRepository navDataRepository)
    {
        ArgumentNullException.ThrowIfNull(station);

        mAtisStation = station;
        mAppConfig = appConfig;
        mAuthTokenManager = authTokenManager;
        mMetarRepository = metarRepository;
        mDownloader = downloader;
        mNavDataRepository = navDataRepository;

        mClientProperties = new ClientProperties(CLIENT_NAME,
            Assembly.GetExecutingAssembly().GetName().Version ??
            throw new ApplicationException("Application version not found"));

        mAirportData = mNavDataRepository.GetAirport(station.Identifier ??
                                                     throw new ApplicationException("Airport identifier not found: " +
                                                         station.Identifier));

        var uniqueId = MachineInfoProvider.GetDefaultProvider().GetMachineGuid();
        mUniqueDeviceIdentifier = uniqueId != null ? Encoding.UTF8.GetString(uniqueId) : "Unknown";

        mFsdFrequency = (int)((mAtisStation.Frequency / 1000) - 100000);

        Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        mFsdPositionUpdateTimer = new System.Timers.Timer();
        mFsdPositionUpdateTimer.Interval = 15000; // 15 seconds
        mFsdPositionUpdateTimer.Elapsed += OnFsdPositionUpdateTimerElapsed;

        mFsdSession = new FsdSession(mClientProperties, SynchronizationContext.Current ?? throw new InvalidOperationException())
        {
            IgnoreUnknownPackets = true
        };
        mFsdSession.NetworkConnected += OnNetworkConnected;
        mFsdSession.NetworkDisconnected += OnNetworkDisconnected;
        mFsdSession.NetworkConnectionFailed += OnNetworkConnectionFailed;
        mFsdSession.NetworkError += OnNetworkError;
        mFsdSession.ProtocolErrorReceived += OnProtocolErrorReceived;
        mFsdSession.ServerIdentificationReceived += OnServerIdentificationReceived;
        mFsdSession.ClientQueryReceived += OnClientQueryReceived;
        mFsdSession.ClientQueryResponseReceived += OnClientQueryResponseReceived;
        mFsdSession.KillRequestReceived += OnKillRequestReceived;
        mFsdSession.TextMessageReceived += OnTextMessageReceived;
        mFsdSession.AtcPositionReceived += OnATCPositionReceived;
        mFsdSession.DeleteAtcReceived += OnDeleteATCReceived;
        mFsdSession.ChangeServerReceived += OnChangeServerReceived;
        mFsdSession.RawDataReceived += OnRawDataReceived;
        mFsdSession.RawDataSent += OnRawDataSent;

        MessageBus.Current.Listen<MetarReceived>().Subscribe(evt =>
        {
            if (evt.Metar.Icao == station.Identifier)
            {
                var isNewMetar = !string.IsNullOrEmpty(mPreviousMetar) &&
                                 evt.Metar.RawMetar?.Trim() != mPreviousMetar?.Trim();
                if (mPreviousMetar != evt.Metar.RawMetar)
                {
                    MetarResponseReceived(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                    mPreviousMetar = evt.Metar.RawMetar;
                    mDecodedMetar = evt.Metar;
                }
            }
        });

        MessageBus.Current.Listen<SessionEnded>().Subscribe((_) => { Disconnect(); });
    }

    private void OnDeleteATCReceived(object? sender, DataReceivedEventArgs<PDUDeleteATC> e)
    {
        mSubscribers.Remove(e.PDU.From.ToUpperInvariant());
        mEuroscopeSubscribers.Remove(e.PDU.From.ToUpperInvariant());
    }

    private void OnNetworkConnectionFailed(object? sender, NetworkEventArgs e)
    {
        NetworkConnectionFailed(this, EventArgs.Empty);
    }

    private void OnRawDataSent(object? sender, RawDataEventArgs e)
    {
        Log.Debug(">> " + e.Data.Trim('\n').Trim('\r'));
    }

    private void OnRawDataReceived(object? sender, RawDataEventArgs e)
    {
        Log.Debug("<< " + e.Data.Trim('\n').Trim('\r'));
    }

    private void OnNetworkConnected(object? sender, NetworkEventArgs e)
    {
        NetworkConnected(this, EventArgs.Empty);
    }

    private void OnNetworkDisconnected(object? sender, NetworkEventArgs e)
    {
        if (mAtisStation?.Identifier != null)
            mMetarRepository.RemoveMetar(mAtisStation.Identifier);
        
        NetworkDisconnected(this, EventArgs.Empty);
        mPreviousMetar = "";
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        NetworkErrorReceived(this, new NetworkErrorReceived(e.Error));
    }

    private void OnProtocolErrorReceived(object? sender, DataReceivedEventArgs<PDUProtocolError> e)
    {
        switch (e.PDU.ErrorType)
        {
            case NetworkError.CallsignInUse:
                NetworkErrorReceived(this, new NetworkErrorReceived("ATIS callsign already in use."));
                break;
            case NetworkError.UnauthorizedSoftware:
                NetworkErrorReceived(this, new NetworkErrorReceived("Unauthorized client software."));
                return;
            case NetworkError.InvalidLogon:
                NetworkErrorReceived(this, new NetworkErrorReceived("Invalid User ID or Password. Please try again."));
                break;
            case NetworkError.CertificateSuspended:
                NetworkErrorReceived(this, new NetworkErrorReceived("User suspended."));
                break;
            case NetworkError.RequestedLevelTooHigh:
                NetworkErrorReceived(this, new NetworkErrorReceived("Invalid Network Rating for User ID."));
                break;
            default:
                if (e.PDU.Fatal)
                    NetworkErrorReceived(this, new NetworkErrorReceived(e.PDU.Message));
                break;
        }
    }

    private void OnServerIdentificationReceived(object? sender, DataReceivedEventArgs<PDUServerIdentification> e)
    {
        SendClientIdentification();
        SendAddAtc();
        SendAtcPositionPacket();
        mFsdPositionUpdateTimer.Start();
    }

    private void OnClientQueryReceived(object? sender, DataReceivedEventArgs<PDUClientQuery> e)
    {
        switch (e.PDU.QueryType)
        {
            case ClientQueryType.Capabilities:
                mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.Capabilities, [
                    "VERSION=1",
                    "ATCINFO=1"
                ]));
                break;
            case ClientQueryType.RealName:
                mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.RealName, [
                    mAppConfig.Name,
                    $"vATIS Connection " + mAtisStation!.Identifier,
                    ((int)mAppConfig.NetworkRating).ToString()
                ]));
                break;
            case ClientQueryType.ATIS:
                var num = 0;
                if (mAtisStation != null && !string.IsNullOrEmpty(mAtisStation.TextAtis))
                {
                    // break up the text into 64 characters per line
                    var regex = new Regex(@"(.{1,64})(?:\s|$)");
                    var collection = regex.Matches(mAtisStation.TextAtis).Select(x => x.Groups[1].Value).ToList();
                    foreach (var line in collection)
                    {
                        num++;
                        mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.ATIS,
                            ["T", line.Replace(":", "").ToUpperInvariant()]));
                    }
                }

                num++;
                mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.ATIS,
                    ["E", num.ToString()]));
                if (mAtisStation?.AtisLetter != null)
                {
                    mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.ATIS,
                        ["A", mAtisStation.AtisLetter]));
                }

                break;
            case ClientQueryType.INF:
                var msg =
                    $"CID={mAppConfig.UserId.Trim()} {mClientProperties.Name} {mClientProperties.Version} IP={mPublicIp} SYS_UID={mUniqueDeviceIdentifier} FSVER=N/A LT={mAirportData?.Latitude} LO={mAirportData?.Longitude} AL=0 {mAppConfig.Name}";
                mFsdSession.SendPdu(new PDUTextMessage(Callsign, e.PDU.From, msg));
                mFsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.PDU.From, ClientQueryType.INF, [msg]));
                break;
        }
    }

    private void OnClientQueryResponseReceived(object? sender, DataReceivedEventArgs<PDUClientQueryResponse> e)
    {
        switch (e.PDU.QueryType)
        {
            case ClientQueryType.PublicIP:
                mPublicIp = ((e.PDU.Payload.Count > 0) ? e.PDU.Payload[0] : "");
                break;
            case ClientQueryType.Capabilities:
                if (!mClientCapabilitiesReceived.Contains(e.PDU.From))
                {
                    mClientCapabilitiesReceived.Add(e.PDU.From);
                }

                if (e.PDU.Payload.Contains("ONGOINGCOORD=1"))
                {
                    if (!mEuroscopeSubscribers.Contains(e.PDU.From))
                    {
                        mEuroscopeSubscribers.Add(e.PDU.From);
                    }
                }

                break;
        }
    }

    private void OnKillRequestReceived(object? sender, DataReceivedEventArgs<PDUKillRequest> e)
    {
        KillRequestReceived(this, new KillRequestReceived(e.PDU.Reason));
        Disconnect();
    }

    private void OnTextMessageReceived(object? sender, DataReceivedEventArgs<PDUTextMessage> e)
    {
        var from = e.PDU.From.ToUpperInvariant();
        var message = e.PDU.Message.ToUpperInvariant();

        switch (message)
        {
            case "SUBSCRIBE":
                if (!mSubscribers.Contains(from))
                {
                    mSubscribers.Add(from);
                    mFsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You are now subscribed to receive {Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {Callsign} with the message UNSUBSCRIBE."));
                }
                else
                {
                    mFsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You are already subscribed to {Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {Callsign} with the message UNSUBSCRIBE."));
                }

                break;
            case "UNSUBSCRIBE":
                if (mSubscribers.Contains(from))
                {
                    mSubscribers.Remove(from);
                    mFsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You have been unsubscribed from {Callsign} update notifications. You may subscribe again by sending a private message to {Callsign} with the message SUBSCRIBE."));
                }

                break;
        }
    }

    private void OnATCPositionReceived(object? sender, DataReceivedEventArgs<PDUATCPosition> e)
    {
        if (!mClientCapabilitiesReceived.Contains(e.PDU.From))
        {
            mFsdSession.SendPdu(new PDUClientQuery(Callsign, e.PDU.From, ClientQueryType.Capabilities, []));
        }
    }

    private void OnChangeServerReceived(object? sender, DataReceivedEventArgs<PDUChangeServer> e)
    {
        ChangeServerReceived(this, new ClientEventArgs<string>(e.PDU.NewServer));
    }

    private void OnFsdPositionUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        SendAtcPositionPacket();
    }

    public async Task Connect(string? serverAddress = null)
    {
        ArgumentNullException.ThrowIfNull(mAtisStation);

        await mAuthTokenManager.GetAuthToken();

        var bestServer = await mDownloader.DownloadStringAsync(VATSIM_SERVER_ENDPOINT);
        if (!string.IsNullOrEmpty(bestServer))
        {
            if (Regex.IsMatch(bestServer, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.CultureInvariant))
            {
                mFsdSession.Connect(serverAddress ?? bestServer, 6809);
                mPreviousMetar = "";
            }
            else
            {
                throw new Exception("Invalid server address format: " + bestServer);
            }
        }
        else
        {
            throw new Exception("Server address returned null.");
        }

        ArgumentNullException.ThrowIfNull(mAtisStation.Identifier);
        await mMetarRepository.GetMetar(mAtisStation.Identifier, monitor: true);
    }

    public void Disconnect()
    {
        mFsdSession.SendPdu(new PDUDeleteATC(Callsign, mAppConfig.UserId.Trim()));
        mFsdSession.Disconnect();
        mFsdPositionUpdateTimer.Stop();
        mPreviousMetar = "";
        mClientCapabilitiesReceived.Clear();
        mSubscribers.Clear();
        mEuroscopeSubscribers.Clear();
    }

    private void SendClientIdentification()
    {
        mFsdSession.SendPdu(new PDUClientIdentification(Callsign, CLIENT_ID, mClientProperties.Name,
            mClientProperties.Version.Major, mClientProperties.Version.Minor, mAppConfig.UserId.Trim(),
            mUniqueDeviceIdentifier, ""));
    }

    private void SendAddAtc()
    {
        mFsdSession.SendPdu(new PDUAddATC(Callsign, mAppConfig.Name, mAppConfig.UserId.Trim(),
            mAuthTokenManager.AuthToken ?? throw new ApplicationException("AuthToken is null or empty."),
            mAppConfig.NetworkRating, ProtocolRevision.VatsimAuth));

        mFsdSession.SendPdu(new PDUClientQuery(Callsign, PDUBase.SERVER_CALLSIGN, ClientQueryType.PublicIP));
    }

    private void SendAtcPositionPacket()
    {
        if (mAirportData == null)
            throw new ApplicationException("Airport data is null");

        mFsdSession.SendPdu(new PDUATCPosition(Callsign, mFsdFrequency, NetworkFacility.TWR, 50,
            mAppConfig.NetworkRating, mAirportData.Latitude, mAirportData.Longitude));
    }

    public void SendSubscriberNotification(char currentAtisLetter)
    {
        if (mDecodedMetar == null)
            return;

        if (mAtisStation == null)
            return;

        foreach (var subscriber in mSubscribers.ToList())
        {
            mFsdSession.SendPdu(new PDUTextMessage(Callsign, subscriber,
                $"***{mAtisStation.Identifier.ToUpperInvariant()} ATIS UPDATE: {currentAtisLetter} " +
                $"{mDecodedMetar.SurfaceWind?.RawValue?.Trim()} - {mDecodedMetar.Pressure?.RawValue?.Trim()}"));
        }

        foreach (var subscriber in mEuroscopeSubscribers.ToList())
        {
            mFsdSession.SendPdu(new PDUTextMessage(Callsign, subscriber,
                $"ATIS info:{mAtisStation.Identifier.ToUpperInvariant()}:{currentAtisLetter}:"));
        }

        mFsdSession.SendPdu(new PDUClientQuery(Callsign, PDUBase.CLIENT_QUERY_BROADCAST_RECIPIENT,
            ClientQueryType.NewATIS, 
            [$"{currentAtisLetter}:{mDecodedMetar.SurfaceWind?.RawValue?.Trim()} {mDecodedMetar.Pressure?.RawValue?.Trim()}"]));
    }
}