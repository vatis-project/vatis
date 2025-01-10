using System;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Networking;

public interface INetworkConnection
{
    string Callsign { get; }

    bool IsConnected { get; }

    event EventHandler NetworkConnected;

    event EventHandler NetworkDisconnected;

    event EventHandler NetworkConnectionFailed;

    event EventHandler<MetarResponseReceived> MetarResponseReceived;

    event EventHandler<NetworkErrorReceived> NetworkErrorReceived;

    event EventHandler<KillRequestReceived> KillRequestReceived;

    event EventHandler<ClientEventArgs<string>> ChangeServerReceived;

    Task Connect(string? serverAddress = null);

    void Disconnect();

    void SendSubscriberNotification(char atisLetter);
}