using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ReactiveUI;
using Serilog;
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
using Timer = System.Timers.Timer;

namespace Vatsim.Vatis.Networking;

public class NetworkConnection : INetworkConnection
{
    private const string VatsimServerEndpoint = "http://fsd.vatsim.net";
    private const string ClientName = "vATIS";
    private const ushort ClientId = 0x579f;
    private readonly Airport? _airportData;
    private readonly IAppConfig _appConfig;

    private readonly AtisStation? _atisStation;
    private readonly IAuthTokenManager _authTokenManager;
    private readonly List<string> _clientCapabilitiesReceived = [];
    private readonly ClientProperties _clientProperties;
    private readonly IDownloader _downloader;
    private readonly List<string> _euroscopeSubscribers = [];
    private readonly int _fsdFrequency;
    private readonly Timer _fsdPositionUpdateTimer;
    private readonly FsdSession _fsdSession;
    private readonly IMetarRepository _metarRepository;
    private readonly List<string> _subscribers = [];
    private readonly string _uniqueDeviceIdentifier;
    private DecodedMetar? _decodedMetar;
    private string? _previousMetar;
    private string? _publicIp;

    public NetworkConnection(
        AtisStation station,
        IAppConfig appConfig,
        IAuthTokenManager authTokenManager,
        IMetarRepository metarRepository,
        IDownloader downloader,
        INavDataRepository navDataRepository,
        IClientAuth clientAuth)
    {
        ArgumentNullException.ThrowIfNull(station);

        this._atisStation = station;
        this._appConfig = appConfig;
        this._authTokenManager = authTokenManager;
        this._metarRepository = metarRepository;
        this._downloader = downloader;

        this._clientProperties = new ClientProperties(
            ClientName,
            Assembly.GetExecutingAssembly().GetName().Version ??
            throw new ApplicationException("Application version not found"));

        this._airportData = navDataRepository.GetAirport(
            station.Identifier ??
            throw new ApplicationException(
                "Airport identifier not found: " +
                station.Identifier));

        var uniqueId = MachineInfoProvider.GetDefaultProvider().GetMachineGuid();
        this._uniqueDeviceIdentifier = uniqueId != null
            ? Encoding.UTF8.GetString(uniqueId).Replace("\r", "").Replace("\n", "").Trim()
            : "Unknown";

        this._fsdFrequency = (int)((this._atisStation.Frequency / 1000) - 100000);

        this.Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType)
        };

        this._fsdPositionUpdateTimer = new Timer();
        this._fsdPositionUpdateTimer.Interval = 15000; // 15 seconds
        this._fsdPositionUpdateTimer.Elapsed += this.OnFsdPositionUpdateTimerElapsed;

        this._fsdSession = new FsdSession(
            clientAuth,
            this._clientProperties,
            SynchronizationContext.Current ?? throw new InvalidOperationException())
        {
            IgnoreUnknownPackets = true
        };
        this._fsdSession.NetworkConnected += this.OnNetworkConnected;
        this._fsdSession.NetworkDisconnected += this.OnNetworkDisconnected;
        this._fsdSession.NetworkConnectionFailed += this.OnNetworkConnectionFailed;
        this._fsdSession.NetworkError += this.OnNetworkError;
        this._fsdSession.ProtocolErrorReceived += this.OnProtocolErrorReceived;
        this._fsdSession.ServerIdentificationReceived += this.OnServerIdentificationReceived;
        this._fsdSession.ClientQueryReceived += this.OnClientQueryReceived;
        this._fsdSession.ClientQueryResponseReceived += this.OnClientQueryResponseReceived;
        this._fsdSession.KillRequestReceived += this.OnKillRequestReceived;
        this._fsdSession.TextMessageReceived += this.OnTextMessageReceived;
        this._fsdSession.AtcPositionReceived += this.OnATCPositionReceived;
        this._fsdSession.DeleteAtcReceived += this.OnDeleteATCReceived;
        this._fsdSession.ChangeServerReceived += this.OnChangeServerReceived;
        this._fsdSession.RawDataReceived += this.OnRawDataReceived;
        this._fsdSession.RawDataSent += this.OnRawDataSent;

        MessageBus.Current.Listen<MetarReceived>().Subscribe(
            evt =>
            {
                if (evt.Metar.Icao == station.Identifier)
                {
                    var isNewMetar = !string.IsNullOrEmpty(this._previousMetar) &&
                                     evt.Metar.RawMetar?.Trim() != this._previousMetar?.Trim();
                    if (this._previousMetar != evt.Metar.RawMetar)
                    {
                        this.MetarResponseReceived(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                        this._previousMetar = evt.Metar.RawMetar;
                        this._decodedMetar = evt.Metar;
                    }
                }
            });

        MessageBus.Current.Listen<SessionEnded>().Subscribe(_ => { this.Disconnect(); });
    }

    public event EventHandler NetworkConnected = delegate { };

    public event EventHandler NetworkDisconnected = delegate { };

    public event EventHandler NetworkConnectionFailed = delegate { };

    public event EventHandler<MetarResponseReceived> MetarResponseReceived = delegate { };

    public event EventHandler<NetworkErrorReceived> NetworkErrorReceived = delegate { };

    public event EventHandler<KillRequestReceived> KillRequestReceived = delegate { };

    public event EventHandler<ClientEventArgs<string>> ChangeServerReceived = delegate { };

    public string Callsign { get; }

    public bool IsConnected => this._fsdSession.Connected;

    public async Task Connect(string? serverAddress = null)
    {
        ArgumentNullException.ThrowIfNull(this._atisStation);

        await this._authTokenManager.GetAuthToken();

        var bestServer = await this._downloader.DownloadStringAsync(VatsimServerEndpoint);
        if (!string.IsNullOrEmpty(bestServer))
        {
            if (Regex.IsMatch(bestServer, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.CultureInvariant))
            {
                this._fsdSession.Connect(serverAddress ?? bestServer, 6809);
                this._previousMetar = "";
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

        ArgumentNullException.ThrowIfNull(this._atisStation.Identifier);
        await this._metarRepository.GetMetar(this._atisStation.Identifier, true);
    }

    public void Disconnect()
    {
        this._fsdSession.SendPdu(new PDUDeleteATC(this.Callsign, this._appConfig.UserId.Trim()));
        this._fsdSession.Disconnect();
        this._fsdPositionUpdateTimer.Stop();
        this._previousMetar = "";
        this._clientCapabilitiesReceived.Clear();
        this._subscribers.Clear();
        this._euroscopeSubscribers.Clear();
    }

    public void SendSubscriberNotification(char currentAtisLetter)
    {
        if (this._decodedMetar == null)
        {
            return;
        }

        if (this._atisStation == null)
        {
            return;
        }

        foreach (var subscriber in this._subscribers.ToList())
        {
            this._fsdSession.SendPdu(
                new PDUTextMessage(
                    this.Callsign,
                    subscriber,
                    $"***{this._atisStation.Identifier.ToUpperInvariant()} ATIS UPDATE: {currentAtisLetter} " +
                    $"{this._decodedMetar.SurfaceWind?.RawValue?.Trim()} - {this._decodedMetar.Pressure?.RawValue?.Trim()}"));
        }

        foreach (var subscriber in this._euroscopeSubscribers.ToList())
        {
            this._fsdSession.SendPdu(
                new PDUTextMessage(
                    this.Callsign,
                    subscriber,
                    $"ATIS info:{this._atisStation.Identifier.ToUpperInvariant()}:{currentAtisLetter}:"));
        }

        this._fsdSession.SendPdu(
            new PDUClientQuery(
                this.Callsign,
                PDUBase.CLIENT_QUERY_BROADCAST_RECIPIENT,
                ClientQueryType.NewAtis,
                [
                    $"{currentAtisLetter}:{this._decodedMetar.SurfaceWind?.RawValue?.Trim()} {this._decodedMetar.Pressure?.RawValue?.Trim()}"
                ]));
    }

    private void OnDeleteATCReceived(object? sender, DataReceivedEventArgs<PDUDeleteATC> e)
    {
        this._subscribers.Remove(e.Pdu.From.ToUpperInvariant());
        this._euroscopeSubscribers.Remove(e.Pdu.From.ToUpperInvariant());
    }

    private void OnNetworkConnectionFailed(object? sender, NetworkEventArgs e)
    {
        this.NetworkConnectionFailed(this, EventArgs.Empty);
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
        this.NetworkConnected(this, EventArgs.Empty);
    }

    private void OnNetworkDisconnected(object? sender, NetworkEventArgs e)
    {
        if (this._atisStation?.Identifier != null)
        {
            this._metarRepository.RemoveMetar(this._atisStation.Identifier);
        }

        this.NetworkDisconnected(this, EventArgs.Empty);
        this._previousMetar = "";
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        this.NetworkErrorReceived(this, new NetworkErrorReceived(e.Error));
    }

    private void OnProtocolErrorReceived(object? sender, DataReceivedEventArgs<PDUProtocolError> e)
    {
        switch (e.Pdu.ErrorType)
        {
            case NetworkError.CallsignInUse:
                this.NetworkErrorReceived(this, new NetworkErrorReceived("ATIS callsign already in use."));
                break;
            case NetworkError.UnauthorizedSoftware:
                this.NetworkErrorReceived(this, new NetworkErrorReceived("Unauthorized client software."));
                return;
            case NetworkError.InvalidLogon:
                this.NetworkErrorReceived(
                    this,
                    new NetworkErrorReceived("Invalid User ID or Password. Please try again."));
                break;
            case NetworkError.CertificateSuspended:
                this.NetworkErrorReceived(this, new NetworkErrorReceived("User suspended."));
                break;
            case NetworkError.RequestedLevelTooHigh:
                this.NetworkErrorReceived(this, new NetworkErrorReceived("Invalid Network Rating for User ID."));
                break;
            default:
                if (e.Pdu.Fatal)
                {
                    this.NetworkErrorReceived(this, new NetworkErrorReceived(e.Pdu.Message));
                }

                break;
        }
    }

    private void OnServerIdentificationReceived(object? sender, DataReceivedEventArgs<PDUServerIdentification> e)
    {
        this.SendClientIdentification();
        this.SendAddAtc();
        this.SendAtcPositionPacket();
        this._fsdPositionUpdateTimer.Start();
    }

    private void OnClientQueryReceived(object? sender, DataReceivedEventArgs<PDUClientQuery> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.Capabilities:
                this._fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.Capabilities,
                        [
                            "VERSION=1",
                            "ATCINFO=1"
                        ]));
                break;
            case ClientQueryType.RealName:
                this._fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.RealName,
                        [
                            this._appConfig.Name,
                            "vATIS Connection " + this._atisStation!.Identifier,
                            ((int)this._appConfig.NetworkRating).ToString()
                        ]));
                break;
            case ClientQueryType.Atis:
                var num = 0;
                if (this._atisStation != null && !string.IsNullOrEmpty(this._atisStation.TextAtis))
                {
                    // break up the text into 64 characters per line
                    var regex = new Regex(@"(.{1,64})(?:\s|$)");
                    var collection = regex.Matches(this._atisStation.TextAtis).Select(x => x.Groups[1].Value).ToList();
                    foreach (var line in collection)
                    {
                        num++;
                        this._fsdSession.SendPdu(
                            new PDUClientQueryResponse(
                                this.Callsign,
                                e.Pdu.From,
                                ClientQueryType.Atis,
                                ["T", line.Replace(":", "").ToUpperInvariant()]));
                    }
                }

                num++;
                this._fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.Atis,
                        ["E", num.ToString()]));
                this._fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.Atis,
                        ["A", this._atisStation?.AtisLetter.ToString() ?? string.Empty]));

                break;
            case ClientQueryType.Inf:
                var msg =
                    $"CID={this._appConfig.UserId.Trim()} {this._clientProperties.Name} {this._clientProperties.Version} IP={this._publicIp} SYS_UID={this._uniqueDeviceIdentifier} FSVER=N/A LT={this._airportData?.Latitude} LO={this._airportData?.Longitude} AL=0 {this._appConfig.Name}";
                this._fsdSession.SendPdu(new PDUTextMessage(this.Callsign, e.Pdu.From, msg));
                this._fsdSession.SendPdu(
                    new PDUClientQueryResponse(this.Callsign, e.Pdu.From, ClientQueryType.Inf, [msg]));
                break;
        }
    }

    private void OnClientQueryResponseReceived(object? sender, DataReceivedEventArgs<PDUClientQueryResponse> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.PublicIp:
                this._publicIp = e.Pdu.Payload.Count > 0 ? e.Pdu.Payload[0] : "";
                break;
            case ClientQueryType.Capabilities:
                if (!this._clientCapabilitiesReceived.Contains(e.Pdu.From))
                {
                    this._clientCapabilitiesReceived.Add(e.Pdu.From);
                }

                if (e.Pdu.Payload.Contains("ONGOINGCOORD=1"))
                {
                    if (!this._euroscopeSubscribers.Contains(e.Pdu.From))
                    {
                        this._euroscopeSubscribers.Add(e.Pdu.From);
                    }
                }

                break;
        }
    }

    private void OnKillRequestReceived(object? sender, DataReceivedEventArgs<PDUKillRequest> e)
    {
        this.KillRequestReceived(this, new KillRequestReceived(e.Pdu.Reason));
        this.Disconnect();
    }

    private void OnTextMessageReceived(object? sender, DataReceivedEventArgs<PDUTextMessage> e)
    {
        var from = e.Pdu.From.ToUpperInvariant();
        var message = e.Pdu.Message.ToUpperInvariant();

        switch (message)
        {
            case "SUBSCRIBE":
                if (!this._subscribers.Contains(from))
                {
                    this._subscribers.Add(from);
                    this._fsdSession.SendPdu(
                        new PDUTextMessage(
                            this.Callsign,
                            from,
                            $"You are now subscribed to receive {this.Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {this.Callsign} with the message UNSUBSCRIBE."));
                }
                else
                {
                    this._fsdSession.SendPdu(
                        new PDUTextMessage(
                            this.Callsign,
                            from,
                            $"You are already subscribed to {this.Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {this.Callsign} with the message UNSUBSCRIBE."));
                }

                break;
            case "UNSUBSCRIBE":
                if (this._subscribers.Contains(from))
                {
                    this._subscribers.Remove(from);
                    this._fsdSession.SendPdu(
                        new PDUTextMessage(
                            this.Callsign,
                            from,
                            $"You have been unsubscribed from {this.Callsign} update notifications. You may subscribe again by sending a private message to {this.Callsign} with the message SUBSCRIBE."));
                }

                break;
        }
    }

    private void OnATCPositionReceived(object? sender, DataReceivedEventArgs<PDUATCPosition> e)
    {
        if (!this._clientCapabilitiesReceived.Contains(e.Pdu.From))
        {
            this._fsdSession.SendPdu(new PDUClientQuery(this.Callsign, e.Pdu.From, ClientQueryType.Capabilities, []));
        }
    }

    private void OnChangeServerReceived(object? sender, DataReceivedEventArgs<PDUChangeServer> e)
    {
        this.ChangeServerReceived(this, new ClientEventArgs<string>(e.Pdu.NewServer));
    }

    private void OnFsdPositionUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.SendAtcPositionPacket();
    }

    private void SendClientIdentification()
    {
        this._fsdSession.SendPdu(
            new PDUClientIdentification(
                this.Callsign,
                ClientId,
                this._clientProperties.Name,
                this._clientProperties.Version.Major,
                this._clientProperties.Version.Minor,
                this._appConfig.UserId.Trim(),
                this._uniqueDeviceIdentifier,
                ""));
    }

    private void SendAddAtc()
    {
        this._fsdSession.SendPdu(
            new PDUAddATC(
                this.Callsign,
                this._appConfig.Name,
                this._appConfig.UserId.Trim(),
                this._authTokenManager.AuthToken ?? throw new ApplicationException("AuthToken is null or empty."),
                this._appConfig.NetworkRating,
                ProtocolRevision.VatsimAuth));

        this._fsdSession.SendPdu(new PDUClientQuery(this.Callsign, PDUBase.SERVER_CALLSIGN, ClientQueryType.PublicIp));
    }

    private void SendAtcPositionPacket()
    {
        if (this._airportData == null)
        {
            throw new ApplicationException("Airport data is null");
        }

        this._fsdSession.SendPdu(
            new PDUATCPosition(
                this.Callsign,
                this._fsdFrequency,
                NetworkFacility.Twr,
                50,
                this._appConfig.NetworkRating,
                this._airportData.Latitude,
                this._airportData.Longitude));
    }
}