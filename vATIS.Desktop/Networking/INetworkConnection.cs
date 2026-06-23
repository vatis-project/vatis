// <copyright file="INetworkConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Represents an interface defining the required functionalities for a VATSIM network connection.
/// </summary>
public interface INetworkConnection
{
    /// <summary>
    /// Occurs when a network connection is successfully established.
    /// </summary>
    public event EventHandler NetworkConnected;

    /// <summary>
    /// Occurs when the network connection is terminated or unavailable.
    /// </summary>
    public event EventHandler<NetworkDisconnectedReceived> NetworkDisconnected;

    /// <summary>
    /// Occurs when a network connection attempt fails.
    /// </summary>
    public event EventHandler NetworkConnectionFailed;

    /// <summary>
    /// Occurs when a response to a METAR request is received.
    /// </summary>
    public event EventHandler<MetarResponseReceived> MetarResponseReceived;

    /// <summary>
    /// Occurs when a network error is received.
    /// </summary>
    public event EventHandler<NetworkErrorReceived> NetworkErrorReceived;

    /// <summary>
    /// Occurs when a kill request is received.
    /// </summary>
    public event EventHandler<KillRequestReceived> KillRequestReceived;

    /// <summary>
    /// Occurs when a server change notification is received.
    /// </summary>
    public event EventHandler<ClientEventArgs<string>> ChangeServerReceived;

    /// <summary>
    /// Occurs when a PONG event is received.
    /// </summary>
    public event EventHandler PongReceived;

    /// <summary>
    /// Gets the callsign associated with the network connection.
    /// </summary>
    public string Callsign { get; }

    /// <summary>
    /// Gets a value indicating whether the network connection is established.
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Connects to the specified server address.
    /// </summary>
    /// <param name="serverAddress">The server address to connect to. If null, the default server address is used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Connect(string? serverAddress = null);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    public void Disconnect();

    /// <summary>
    /// Sends a subscriber notification with the specified ATIS letter.
    /// </summary>
    /// <param name="atisLetter">The ATIS letter to include in the notification.</param>
    public void SendSubscriberNotification(char atisLetter);
}
