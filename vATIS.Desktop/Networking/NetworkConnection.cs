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
    private const string VatsimServerEndpoint = "http://fsd.vatsim.net";
    private const string ClientName = "vATIS";
    private const ushort ClientId = 0x579f;

    private readonly AtisStation? _atisStation;
    private readonly FsdSession _fsdSession;
    private readonly ClientProperties _clientProperties;
    private readonly IAppConfig _appConfig;
    private readonly IAuthTokenManager _authTokenManager;
    private readonly IMetarRepository _metarRepository;
    private readonly IDownloader _downloader;
    private readonly System.Timers.Timer _fsdPositionUpdateTimer;
    private readonly string _uniqueDeviceIdentifier;
    private string? _publicIp;
    private string? _previousMetar;
    private readonly List<string> _subscribers = [];
    private readonly List<string> _euroscopeSubscribers = [];
    private readonly List<string> _clientCapabilitiesReceived = [];
    private readonly Airport? _airportData;
    private readonly int _fsdFrequency;
    private DecodedMetar? _decodedMetar;

    public event EventHandler NetworkConnected = delegate { };
    public event EventHandler NetworkDisconnected = delegate { };
    public event EventHandler NetworkConnectionFailed = delegate { };
    public event EventHandler<MetarResponseReceived> MetarResponseReceived = delegate { };
    public event EventHandler<NetworkErrorReceived> NetworkErrorReceived = delegate { };
    public event EventHandler<KillRequestReceived> KillRequestReceived = delegate { };
    public event EventHandler<ClientEventArgs<string>> ChangeServerReceived = delegate { };

    public string Callsign { get; }
    public bool IsConnected => _fsdSession.Connected;

    public NetworkConnection(AtisStation station, IAppConfig appConfig, IAuthTokenManager authTokenManager,
        IMetarRepository metarRepository, IDownloader downloader, INavDataRepository navDataRepository, IClientAuth clientAuth)
    {
        ArgumentNullException.ThrowIfNull(station);

        _atisStation = station;
        _appConfig = appConfig;
        _authTokenManager = authTokenManager;
        _metarRepository = metarRepository;
        _downloader = downloader;

        _clientProperties = new ClientProperties(ClientName,
            Assembly.GetExecutingAssembly().GetName().Version ??
            throw new ApplicationException("Application version not found"));

        _airportData = navDataRepository.GetAirport(station.Identifier ??
                                                    throw new ApplicationException("Airport identifier not found: " +
                                                        station.Identifier));

        var uniqueId = MachineInfoProvider.GetDefaultProvider().GetMachineGuid();
        _uniqueDeviceIdentifier = uniqueId != null
            ? Encoding.UTF8.GetString(uniqueId).Replace("\r", "").Replace("\n", "").Trim()
            : "Unknown";

        _fsdFrequency = (int)((_atisStation.Frequency / 1000) - 100000);

        Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        _fsdPositionUpdateTimer = new System.Timers.Timer();
        _fsdPositionUpdateTimer.Interval = 15000; // 15 seconds
        _fsdPositionUpdateTimer.Elapsed += OnFsdPositionUpdateTimerElapsed;

        _fsdSession = new FsdSession(clientAuth, _clientProperties, SynchronizationContext.Current ?? throw new InvalidOperationException())
        {
            IgnoreUnknownPackets = true
        };
        _fsdSession.NetworkConnected += OnNetworkConnected;
        _fsdSession.NetworkDisconnected += OnNetworkDisconnected;
        _fsdSession.NetworkConnectionFailed += OnNetworkConnectionFailed;
        _fsdSession.NetworkError += OnNetworkError;
        _fsdSession.ProtocolErrorReceived += OnProtocolErrorReceived;
        _fsdSession.ServerIdentificationReceived += OnServerIdentificationReceived;
        _fsdSession.ClientQueryReceived += OnClientQueryReceived;
        _fsdSession.ClientQueryResponseReceived += OnClientQueryResponseReceived;
        _fsdSession.KillRequestReceived += OnKillRequestReceived;
        _fsdSession.TextMessageReceived += OnTextMessageReceived;
        _fsdSession.AtcPositionReceived += OnATCPositionReceived;
        _fsdSession.DeleteAtcReceived += OnDeleteATCReceived;
        _fsdSession.ChangeServerReceived += OnChangeServerReceived;
        _fsdSession.RawDataReceived += OnRawDataReceived;
        _fsdSession.RawDataSent += OnRawDataSent;

        MessageBus.Current.Listen<MetarReceived>().Subscribe(evt =>
        {
            if (evt.Metar.Icao == station.Identifier)
            {
                var isNewMetar = !string.IsNullOrEmpty(_previousMetar) &&
                                 evt.Metar.RawMetar?.Trim() != _previousMetar?.Trim();
                if (_previousMetar != evt.Metar.RawMetar)
                {
                    MetarResponseReceived(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                    _previousMetar = evt.Metar.RawMetar;
                    _decodedMetar = evt.Metar;
                }
            }
        });

        MessageBus.Current.Listen<SessionEnded>().Subscribe((_) => { Disconnect(); });
    }

    private void OnDeleteATCReceived(object? sender, DataReceivedEventArgs<PDUDeleteATC> e)
    {
        _subscribers.Remove(e.Pdu.From.ToUpperInvariant());
        _euroscopeSubscribers.Remove(e.Pdu.From.ToUpperInvariant());
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
        if (_atisStation?.Identifier != null)
            _metarRepository.RemoveMetar(_atisStation.Identifier);

        NetworkDisconnected(this, EventArgs.Empty);
        _previousMetar = "";
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        NetworkErrorReceived(this, new NetworkErrorReceived(e.Error));
    }

    private void OnProtocolErrorReceived(object? sender, DataReceivedEventArgs<PDUProtocolError> e)
    {
        switch (e.Pdu.ErrorType)
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
                if (e.Pdu.Fatal)
                    NetworkErrorReceived(this, new NetworkErrorReceived(e.Pdu.Message));
                break;
        }
    }

    private void OnServerIdentificationReceived(object? sender, DataReceivedEventArgs<PDUServerIdentification> e)
    {
        SendClientIdentification();
        SendAddAtc();
        SendAtcPositionPacket();
        _fsdPositionUpdateTimer.Start();
    }

    private void OnClientQueryReceived(object? sender, DataReceivedEventArgs<PDUClientQuery> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.Capabilities:
                _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.Capabilities, [
                    "VERSION=1",
                    "ATCINFO=1"
                ]));
                break;
            case ClientQueryType.RealName:
                _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.RealName, [
                    _appConfig.Name,
                    $"vATIS Connection " + _atisStation!.Identifier,
                    ((int)_appConfig.NetworkRating).ToString()
                ]));
                break;
            case ClientQueryType.Atis:
                var num = 0;
                if (_atisStation != null && !string.IsNullOrEmpty(_atisStation.TextAtis))
                {
                    // break up the text into 64 characters per line
                    var regex = new Regex(@"(.{1,64})(?:\s|$)");
                    var collection = regex.Matches(_atisStation.TextAtis).Select(x => x.Groups[1].Value).ToList();
                    foreach (var line in collection)
                    {
                        num++;
                        _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.Atis,
                            ["T", line.Replace(":", "").ToUpperInvariant()]));
                    }
                }

                num++;
                _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.Atis,
                    ["E", num.ToString()]));
                _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.Atis,
                    ["A", _atisStation?.AtisLetter.ToString() ?? string.Empty]));

                break;
            case ClientQueryType.Inf:
                var msg =
                    $"CID={_appConfig.UserId.Trim()} {_clientProperties.Name} {_clientProperties.Version} IP={_publicIp} SYS_UID={_uniqueDeviceIdentifier} FSVER=N/A LT={_airportData?.Latitude} LO={_airportData?.Longitude} AL=0 {_appConfig.Name}";
                _fsdSession.SendPdu(new PDUTextMessage(Callsign, e.Pdu.From, msg));
                _fsdSession.SendPdu(new PDUClientQueryResponse(Callsign, e.Pdu.From, ClientQueryType.Inf, [msg]));
                break;
        }
    }

    private void OnClientQueryResponseReceived(object? sender, DataReceivedEventArgs<PDUClientQueryResponse> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.PublicIp:
                _publicIp = ((e.Pdu.Payload.Count > 0) ? e.Pdu.Payload[0] : "");
                break;
            case ClientQueryType.Capabilities:
                if (!_clientCapabilitiesReceived.Contains(e.Pdu.From))
                {
                    _clientCapabilitiesReceived.Add(e.Pdu.From);
                }

                if (e.Pdu.Payload.Contains("ONGOINGCOORD=1"))
                {
                    if (!_euroscopeSubscribers.Contains(e.Pdu.From))
                    {
                        _euroscopeSubscribers.Add(e.Pdu.From);
                    }
                }

                break;
        }
    }

    private void OnKillRequestReceived(object? sender, DataReceivedEventArgs<PDUKillRequest> e)
    {
        KillRequestReceived(this, new KillRequestReceived(e.Pdu.Reason));
        Disconnect();
    }

    private void OnTextMessageReceived(object? sender, DataReceivedEventArgs<PDUTextMessage> e)
    {
        var from = e.Pdu.From.ToUpperInvariant();
        var message = e.Pdu.Message.ToUpperInvariant();

        switch (message)
        {
            case "SUBSCRIBE":
                if (!_subscribers.Contains(from))
                {
                    _subscribers.Add(from);
                    _fsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You are now subscribed to receive {Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {Callsign} with the message UNSUBSCRIBE."));
                }
                else
                {
                    _fsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You are already subscribed to {Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {Callsign} with the message UNSUBSCRIBE."));
                }

                break;
            case "UNSUBSCRIBE":
                if (_subscribers.Contains(from))
                {
                    _subscribers.Remove(from);
                    _fsdSession.SendPdu(new PDUTextMessage(Callsign, from,
                        $"You have been unsubscribed from {Callsign} update notifications. You may subscribe again by sending a private message to {Callsign} with the message SUBSCRIBE."));
                }

                break;
        }
    }

    private void OnATCPositionReceived(object? sender, DataReceivedEventArgs<PDUATCPosition> e)
    {
        if (!_clientCapabilitiesReceived.Contains(e.Pdu.From))
        {
            _fsdSession.SendPdu(new PDUClientQuery(Callsign, e.Pdu.From, ClientQueryType.Capabilities, []));
        }
    }

    private void OnChangeServerReceived(object? sender, DataReceivedEventArgs<PDUChangeServer> e)
    {
        ChangeServerReceived(this, new ClientEventArgs<string>(e.Pdu.NewServer));
    }

    private void OnFsdPositionUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        SendAtcPositionPacket();
    }

    public async Task Connect(string? serverAddress = null)
    {
        ArgumentNullException.ThrowIfNull(_atisStation);

        await _authTokenManager.GetAuthToken();

        var bestServer = await _downloader.DownloadStringAsync(VatsimServerEndpoint);
        if (!string.IsNullOrEmpty(bestServer))
        {
            if (Regex.IsMatch(bestServer, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.CultureInvariant))
            {
                _fsdSession.Connect(serverAddress ?? bestServer, 6809);
                _previousMetar = "";
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

        ArgumentNullException.ThrowIfNull(_atisStation.Identifier);
        await _metarRepository.GetMetar(_atisStation.Identifier, monitor: true);
    }

    public void Disconnect()
    {
        _fsdSession.SendPdu(new PDUDeleteATC(Callsign, _appConfig.UserId.Trim()));
        _fsdSession.Disconnect();
        _fsdPositionUpdateTimer.Stop();
        _previousMetar = "";
        _clientCapabilitiesReceived.Clear();
        _subscribers.Clear();
        _euroscopeSubscribers.Clear();
    }

    private void SendClientIdentification()
    {
        _fsdSession.SendPdu(new PDUClientIdentification(Callsign, ClientId, _clientProperties.Name,
            _clientProperties.Version.Major, _clientProperties.Version.Minor, _appConfig.UserId.Trim(),
            _uniqueDeviceIdentifier, ""));
    }

    private void SendAddAtc()
    {
        _fsdSession.SendPdu(new PDUAddATC(Callsign, _appConfig.Name, _appConfig.UserId.Trim(),
            _authTokenManager.AuthToken ?? throw new ApplicationException("AuthToken is null or empty."),
            _appConfig.NetworkRating, ProtocolRevision.VatsimAuth));

        _fsdSession.SendPdu(new PDUClientQuery(Callsign, PDUBase.SERVER_CALLSIGN, ClientQueryType.PublicIp));
    }

    private void SendAtcPositionPacket()
    {
        if (_airportData == null)
            throw new ApplicationException("Airport data is null");

        _fsdSession.SendPdu(new PDUATCPosition(Callsign, _fsdFrequency, NetworkFacility.Twr, 50,
            _appConfig.NetworkRating, _airportData.Latitude, _airportData.Longitude));
    }

    public void SendSubscriberNotification(char currentAtisLetter)
    {
        if (_decodedMetar == null)
            return;

        if (_atisStation == null)
            return;

        foreach (var subscriber in _subscribers.ToList())
        {
            _fsdSession.SendPdu(new PDUTextMessage(Callsign, subscriber,
                $"***{_atisStation.Identifier.ToUpperInvariant()} ATIS UPDATE: {currentAtisLetter} " +
                $"{_decodedMetar.SurfaceWind?.RawValue?.Trim()} - {_decodedMetar.Pressure?.RawValue?.Trim()}"));
        }

        foreach (var subscriber in _euroscopeSubscribers.ToList())
        {
            _fsdSession.SendPdu(new PDUTextMessage(Callsign, subscriber,
                $"ATIS info:{_atisStation.Identifier.ToUpperInvariant()}:{currentAtisLetter}:"));
        }

        _fsdSession.SendPdu(new PDUClientQuery(Callsign, PDUBase.CLIENT_QUERY_BROADCAST_RECIPIENT,
            ClientQueryType.NewAtis,
            [$"{currentAtisLetter}:{_decodedMetar.SurfaceWind?.RawValue?.Trim()} {_decodedMetar.Pressure?.RawValue?.Trim()}"]));
    }
}
