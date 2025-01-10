// <copyright file="NetworkConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <inheritdoc />
public class NetworkConnection : INetworkConnection
{
    private const string VatsimServerEndpoint = "http://fsd.vatsim.net";
    private const string ClientName = "vATIS";
    private const ushort ClientId = 0x579f;
    private readonly Airport? airportData;
    private readonly IAppConfig appConfig;
    private readonly AtisStation? atisStation;
    private readonly IAuthTokenManager authTokenManager;
    private readonly List<string> clientCapabilitiesReceived = [];
    private readonly ClientProperties clientProperties;
    private readonly IDownloader downloader;
    private readonly List<string> euroscopeSubscribers = [];
    private readonly int fsdFrequency;
    private readonly Timer fsdPositionUpdateTimer;
    private readonly FsdSession fsdSession;
    private readonly IMetarRepository metarRepository;
    private readonly List<string> subscribers = [];
    private readonly string uniqueDeviceIdentifier;
    private DecodedMetar? decodedMetar;
    private string? previousMetar;
    private string? publicIp;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConnection"/> class.
    /// </summary>
    /// <param name="station">The ATIS station associated with the network connection.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="authTokenManager">The authentication token manager.</param>
    /// <param name="metarRepository">The METAR repository used for weather data.</param>
    /// <param name="downloader">The downloader used for data retrieval.</param>
    /// <param name="navDataRepository">The navigation data repository for airport information.</param>
    /// <param name="clientAuth">The client authentication interface.</param>
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

        this.atisStation = station;
        this.appConfig = appConfig;
        this.authTokenManager = authTokenManager;
        this.metarRepository = metarRepository;
        this.downloader = downloader;

        this.clientProperties = new ClientProperties(ClientName, Assembly.GetExecutingAssembly().GetName().Version ?? throw new ApplicationException("Application version not found"));

        this.airportData = navDataRepository.GetAirport(
            station.Identifier ??
            throw new ApplicationException(
                "Airport identifier not found: " +
                station.Identifier));

        var uniqueId = MachineInfoProvider.GetDefaultProvider().GetMachineGuid();
        this.uniqueDeviceIdentifier = uniqueId != null
            ? Encoding.UTF8.GetString(uniqueId).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim()
            : "Unknown";

        this.fsdFrequency = (int)((this.atisStation.Frequency / 1000) - 100000);

        this.Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        this.fsdPositionUpdateTimer = new Timer();
        this.fsdPositionUpdateTimer.Interval = 15000; // 15 seconds
        this.fsdPositionUpdateTimer.Elapsed += this.OnFsdPositionUpdateTimerElapsed;

        this.fsdSession = new FsdSession(
            clientAuth,
            this.clientProperties,
            SynchronizationContext.Current ?? throw new InvalidOperationException())
        {
            IgnoreUnknownPackets = true,
        };
        this.fsdSession.NetworkConnected += this.OnNetworkConnected;
        this.fsdSession.NetworkDisconnected += this.OnNetworkDisconnected;
        this.fsdSession.NetworkConnectionFailed += this.OnNetworkConnectionFailed;
        this.fsdSession.NetworkError += this.OnNetworkError;
        this.fsdSession.ProtocolErrorReceived += this.OnProtocolErrorReceived;
        this.fsdSession.ServerIdentificationReceived += this.OnServerIdentificationReceived;
        this.fsdSession.ClientQueryReceived += this.OnClientQueryReceived;
        this.fsdSession.ClientQueryResponseReceived += this.OnClientQueryResponseReceived;
        this.fsdSession.KillRequestReceived += this.OnKillRequestReceived;
        this.fsdSession.TextMessageReceived += this.OnTextMessageReceived;
        this.fsdSession.AtcPositionReceived += this.OnATCPositionReceived;
        this.fsdSession.DeleteAtcReceived += this.OnDeleteATCReceived;
        this.fsdSession.ChangeServerReceived += this.OnChangeServerReceived;
        this.fsdSession.RawDataReceived += this.OnRawDataReceived;
        this.fsdSession.RawDataSent += this.OnRawDataSent;

        MessageBus.Current.Listen<MetarReceived>().Subscribe(
            evt =>
            {
                if (evt.Metar.Icao == station.Identifier)
                {
                    var isNewMetar = !string.IsNullOrEmpty(this.previousMetar) &&
                                     evt.Metar.RawMetar?.Trim() != this.previousMetar?.Trim();
                    if (this.previousMetar != evt.Metar.RawMetar)
                    {
                        this.MetarResponseReceived(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                        this.previousMetar = evt.Metar.RawMetar;
                        this.decodedMetar = evt.Metar;
                    }
                }
            });

        MessageBus.Current.Listen<SessionEnded>().Subscribe(_ => { this.Disconnect(); });
    }

    /// <inheritdoc/>
    public event EventHandler NetworkConnected = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler NetworkDisconnected = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler NetworkConnectionFailed = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<MetarResponseReceived> MetarResponseReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<NetworkErrorReceived> NetworkErrorReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<KillRequestReceived> KillRequestReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<ClientEventArgs<string>> ChangeServerReceived = (_, _) => { };

    /// <inheritdoc/>
    public string Callsign { get; }

    /// <inheritdoc/>
    public bool IsConnected => this.fsdSession.Connected;

    /// <inheritdoc/>
    public async Task Connect(string? serverAddress = null)
    {
        ArgumentNullException.ThrowIfNull(this.atisStation);

        await this.authTokenManager.GetAuthToken();

        var bestServer = await this.downloader.DownloadStringAsync(VatsimServerEndpoint);
        if (!string.IsNullOrEmpty(bestServer))
        {
            if (Regex.IsMatch(bestServer, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.CultureInvariant))
            {
                this.fsdSession.Connect(serverAddress ?? bestServer, 6809);
                this.previousMetar = string.Empty;
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

        ArgumentNullException.ThrowIfNull(this.atisStation.Identifier);
        await this.metarRepository.GetMetar(this.atisStation.Identifier, true);
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        this.fsdSession.SendPdu(new PDUDeleteATC(this.Callsign, this.appConfig.UserId.Trim()));
        this.fsdSession.Disconnect();
        this.fsdPositionUpdateTimer.Stop();
        this.previousMetar = string.Empty;
        this.clientCapabilitiesReceived.Clear();
        this.subscribers.Clear();
        this.euroscopeSubscribers.Clear();
    }

    /// <inheritdoc/>
    public void SendSubscriberNotification(char currentAtisLetter)
    {
        if (this.decodedMetar == null)
        {
            return;
        }

        if (this.atisStation == null)
        {
            return;
        }

        foreach (var subscriber in this.subscribers.ToList())
        {
            this.fsdSession.SendPdu(
                new PDUTextMessage(
                    this.Callsign,
                    subscriber,
                    $"***{this.atisStation.Identifier.ToUpperInvariant()} ATIS UPDATE: {currentAtisLetter} {this.decodedMetar.SurfaceWind?.RawValue?.Trim()} - {this.decodedMetar.Pressure?.RawValue?.Trim()}"));
        }

        foreach (var subscriber in this.euroscopeSubscribers.ToList())
        {
            this.fsdSession.SendPdu(
                new PDUTextMessage(
                    this.Callsign,
                    subscriber,
                    $"ATIS info:{this.atisStation.Identifier.ToUpperInvariant()}:{currentAtisLetter}:"));
        }

        this.fsdSession.SendPdu(
            new PDUClientQuery(
                this.Callsign,
                PDUBase.CLIENT_QUERY_BROADCAST_RECIPIENT,
                ClientQueryType.NewAtis,
                [
                    $"{currentAtisLetter}:{this.decodedMetar.SurfaceWind?.RawValue?.Trim()} {this.decodedMetar.Pressure?.RawValue?.Trim()}"
                ]));
    }

    private void OnDeleteATCReceived(object? sender, DataReceivedEventArgs<PDUDeleteATC> e)
    {
        this.subscribers.Remove(e.Pdu.From.ToUpperInvariant());
        this.euroscopeSubscribers.Remove(e.Pdu.From.ToUpperInvariant());
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
        if (this.atisStation?.Identifier != null)
        {
            this.metarRepository.RemoveMetar(this.atisStation.Identifier);
        }

        this.NetworkDisconnected(this, EventArgs.Empty);
        this.previousMetar = string.Empty;
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
        this.fsdPositionUpdateTimer.Start();
    }

    private void OnClientQueryReceived(object? sender, DataReceivedEventArgs<PDUClientQuery> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.Capabilities:
                this.fsdSession.SendPdu(
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
                this.fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.RealName,
                        [
                            this.appConfig.Name,
                            "vATIS Connection " + this.atisStation!.Identifier,
                            ((int)this.appConfig.NetworkRating).ToString()
                        ]));
                break;
            case ClientQueryType.Atis:
                var num = 0;
                if (this.atisStation != null && !string.IsNullOrEmpty(this.atisStation.TextAtis))
                {
                    // break up the text into 64 characters per line
                    var regex = new Regex(@"(.{1,64})(?:\s|$)");
                    var collection = regex.Matches(this.atisStation.TextAtis).Select(x => x.Groups[1].Value).ToList();
                    foreach (var line in collection)
                    {
                        num++;
                        this.fsdSession.SendPdu(
                            new PDUClientQueryResponse(
                                this.Callsign,
                                e.Pdu.From,
                                ClientQueryType.Atis,
                                ["T", line.Replace(":", string.Empty).ToUpperInvariant()]));
                    }
                }

                num++;
                this.fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.Atis,
                        ["E", num.ToString()]));
                this.fsdSession.SendPdu(
                    new PDUClientQueryResponse(
                        this.Callsign,
                        e.Pdu.From,
                        ClientQueryType.Atis,
                        ["A", this.atisStation?.AtisLetter.ToString() ?? string.Empty]));

                break;
            case ClientQueryType.Inf:
                var msg =
                    $"CID={this.appConfig.UserId.Trim()} {this.clientProperties.Name} {this.clientProperties.Version} IP={this.publicIp} SYS_UID={this.uniqueDeviceIdentifier} FSVER=N/A LT={this.airportData?.Latitude} LO={this.airportData?.Longitude} AL=0 {this.appConfig.Name}";
                this.fsdSession.SendPdu(new PDUTextMessage(this.Callsign, e.Pdu.From, msg));
                this.fsdSession.SendPdu(
                    new PDUClientQueryResponse(this.Callsign, e.Pdu.From, ClientQueryType.Inf, [msg]));
                break;
        }
    }

    private void OnClientQueryResponseReceived(object? sender, DataReceivedEventArgs<PDUClientQueryResponse> e)
    {
        switch (e.Pdu.QueryType)
        {
            case ClientQueryType.PublicIp:
                this.publicIp = e.Pdu.Payload.Count > 0 ? e.Pdu.Payload[0] : string.Empty;
                break;
            case ClientQueryType.Capabilities:
                if (!this.clientCapabilitiesReceived.Contains(e.Pdu.From))
                {
                    this.clientCapabilitiesReceived.Add(e.Pdu.From);
                }

                if (e.Pdu.Payload.Contains("ONGOINGCOORD=1"))
                {
                    if (!this.euroscopeSubscribers.Contains(e.Pdu.From))
                    {
                        this.euroscopeSubscribers.Add(e.Pdu.From);
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
                if (!this.subscribers.Contains(from))
                {
                    this.subscribers.Add(from);
                    this.fsdSession.SendPdu(
                        new PDUTextMessage(
                            this.Callsign,
                            from,
                            $"You are now subscribed to receive {this.Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {this.Callsign} with the message UNSUBSCRIBE."));
                }
                else
                {
                    this.fsdSession.SendPdu(
                        new PDUTextMessage(
                            this.Callsign,
                            from,
                            $"You are already subscribed to {this.Callsign} update notifications. To stop receiving these notifications, reply or send a private message to {this.Callsign} with the message UNSUBSCRIBE."));
                }

                break;
            case "UNSUBSCRIBE":
                if (this.subscribers.Contains(from))
                {
                    this.subscribers.Remove(from);
                    this.fsdSession.SendPdu(
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
        if (!this.clientCapabilitiesReceived.Contains(e.Pdu.From))
        {
            this.fsdSession.SendPdu(new PDUClientQuery(this.Callsign, e.Pdu.From, ClientQueryType.Capabilities, []));
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
        this.fsdSession.SendPdu(
            new PDUClientIdentification(
                this.Callsign,
                ClientId,
                this.clientProperties.Name,
                this.clientProperties.Version.Major,
                this.clientProperties.Version.Minor,
                this.appConfig.UserId.Trim(),
                this.uniqueDeviceIdentifier,
                string.Empty));
    }

    private void SendAddAtc()
    {
        this.fsdSession.SendPdu(
            new PDUAddATC(
                this.Callsign,
                this.appConfig.Name,
                this.appConfig.UserId.Trim(),
                this.authTokenManager.AuthToken ?? throw new ApplicationException("AuthToken is null or empty."),
                this.appConfig.NetworkRating,
                ProtocolRevision.VatsimAuth));

        this.fsdSession.SendPdu(new PDUClientQuery(this.Callsign, PDUBase.SERVER_CALLSIGN, ClientQueryType.PublicIp));
    }

    private void SendAtcPositionPacket()
    {
        if (this.airportData == null)
        {
            throw new ApplicationException("Airport data is null");
        }

        this.fsdSession.SendPdu(
            new PDUATCPosition(
                this.Callsign,
                this.fsdFrequency,
                NetworkFacility.Twr,
                50,
                this.appConfig.NetworkRating,
                this.airportData.Latitude,
                this.airportData.Longitude));
    }
}
