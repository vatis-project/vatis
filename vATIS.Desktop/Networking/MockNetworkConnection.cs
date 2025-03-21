// <copyright file="MockNetworkConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Represents a mock implementation of a VATSIM network connection for testing purposes.
/// </summary>
public class MockNetworkConnection : INetworkConnection, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IMetarRepository _metarRepository;
    private string? _previousMetar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockNetworkConnection"/> class.
    /// </summary>
    /// <param name="station">The ATIS station associated with this connection.</param>
    /// <param name="metarRepository">The METAR repository used for retrieving METAR information.</param>
    public MockNetworkConnection(AtisStation station, IMetarRepository metarRepository)
    {
        _metarRepository = metarRepository;

        Station = station;
        Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        _disposables.Add(EventBus.Instance.Subscribe<MetarReceived>(evt =>
        {
            if (evt.Metar.Icao == station.Identifier)
            {
                var isNewMetar = !string.IsNullOrEmpty(_previousMetar) &&
                                 evt.Metar.RawMetar?.Trim() != _previousMetar?.Trim();
                if (_previousMetar != evt.Metar.RawMetar)
                {
                    MetarResponseReceived?.Invoke(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                    _previousMetar = evt.Metar.RawMetar;
                }
            }
        }));

        _disposables.Add(EventBus.Instance.Subscribe<SessionEnded>(_ => { Disconnect(); }));
    }

    /// <inheritdoc />
    public event EventHandler? NetworkConnected = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<NetworkDisconnectedReceived>? NetworkDisconnected = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler? NetworkConnectionFailed = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<MetarResponseReceived>? MetarResponseReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<NetworkErrorReceived>? NetworkErrorReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<KillRequestReceived>? KillRequestReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<ClientEventArgs<string>>? ChangeServerReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler? PongReceived = (_, _) => { };

    /// <inheritdoc />
    public string Callsign { get; }

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Gets the ATIS station associated with the network connection.
    /// </summary>
    public AtisStation Station { get; }

    /// <inheritdoc />
    public Task Connect(string? serverAddress)
    {
        _metarRepository.GetMetar(Station.Identifier, monitor: true);

        NetworkConnected?.Invoke(this, EventArgs.Empty);
        IsConnected = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        if (!string.IsNullOrEmpty(Station.Identifier))
            _metarRepository.RemoveMetar(Station.Identifier);

        NetworkDisconnected?.Invoke(this, new NetworkDisconnectedReceived());
        IsConnected = false;

        _previousMetar = null;
    }

    /// <inheritdoc />
    public void SendSubscriberNotification(char atisLetter)
    {
        // ignore
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
