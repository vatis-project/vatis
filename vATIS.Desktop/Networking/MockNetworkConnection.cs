// <copyright file="MockNetworkConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Represents a mock implementation of a network connection for an ATIS station.
/// </summary>
public class MockNetworkConnection : INetworkConnection
{
    private readonly IMetarRepository metarRepository;
    private string? previousMetar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockNetworkConnection"/> class.
    /// </summary>
    /// <param name="station">The ATIS station associated with this network connection.</param>
    /// <param name="metarRepository">The METAR repository used by the network connection for METAR data.</param>
    /// <exception cref="Exception">Thrown if an unknown <see cref="AtisType"/> is provided in the station.</exception>
    public MockNetworkConnection(AtisStation station, IMetarRepository metarRepository)
    {
        this.metarRepository = metarRepository;
        this.IsConnected = false;

        this.Station = station;
        this.Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        MessageBus.Current.Listen<MetarReceived>().Subscribe(
            evt =>
            {
                if (evt.Metar.Icao == station.Identifier)
                {
                    var isNewMetar = !string.IsNullOrEmpty(this.previousMetar) &&
                                     evt.Metar.RawMetar?.Trim() != this.previousMetar?.Trim();
                    if (this.previousMetar != evt.Metar.RawMetar)
                    {
                        this.MetarResponseReceived?.Invoke(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                        this.previousMetar = evt.Metar.RawMetar;
                    }
                }
            });

        MessageBus.Current.Listen<SessionEnded>().Subscribe(_ => { this.Disconnect(); });
    }

    /// <inheritdoc/>
    public event EventHandler? NetworkConnected = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler? NetworkDisconnected = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler? NetworkConnectionFailed = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<MetarResponseReceived>? MetarResponseReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<NetworkErrorReceived>? NetworkErrorReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<KillRequestReceived>? KillRequestReceived = (_, _) => { };

    /// <inheritdoc/>
    public event EventHandler<ClientEventArgs<string>>? ChangeServerReceived = (_, _) => { };

    /// <inheritdoc/>
    public string Callsign { get; }

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Gets the associated ATIS station for the network connection.
    /// </summary>
    public AtisStation Station { get; }

    /// <inheritdoc/>
    public Task Connect(string? serverAddress)
    {
        this.metarRepository.GetMetar(this.Station.Identifier, true);

        this.NetworkConnected?.Invoke(this, EventArgs.Empty);
        this.IsConnected = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        if (!string.IsNullOrEmpty(this.Station.Identifier))
        {
            this.metarRepository.RemoveMetar(this.Station.Identifier);
        }

        this.NetworkDisconnected?.Invoke(this, EventArgs.Empty);
        this.IsConnected = false;

        this.previousMetar = null;
    }

    /// <inheritdoc/>
    public void SendSubscriberNotification(char atisLetter)
    {
        // ignore
    }
}
