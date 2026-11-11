// <copyright file="MockNetworkConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia.Threading;
using Serilog;
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
            // Every station at the airport hears the METAR another station downloads. One that isn't connected
            // must not remember it, or it would treat that METAR as already delivered when it does connect.
            if (IsConnected && evt.Metar.Icao == station.Identifier)
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
        _previousMetar = null;
        NetworkConnected?.Invoke(this, EventArgs.Empty);
        IsConnected = true;

        RequestMetarAsync().SafeFireAndForget(onException: ex => Log.Error(ex, "Failed to request METAR"));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ChangeServer(string serverAddress) => Task.CompletedTask;

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

    private async Task RequestMetarAsync()
    {
        var metar = await _metarRepository.GetMetar(Station.Identifier, monitor: true,
            customUrl: Station.CustomMetarUrl);

        // The repository only publishes a METAR it has just downloaded. When another station at the same airport
        // already cached it, nothing is published, so hand it over here. A freshly published one has already been
        // delivered by the subscription above, which the previous METAR check skips. This is queued on the UI
        // thread so it lands after the station has processed NetworkConnected, which it ignores METARs before.
        if (metar?.RawMetar == null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_previousMetar == metar.RawMetar)
                return;

            MetarResponseReceived?.Invoke(this, new MetarResponseReceived(metar, false));
            _previousMetar = metar.RawMetar;
        });
    }
}
