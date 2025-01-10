using System;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Networking;

public class MockNetworkConnection : INetworkConnection
{
    private readonly IMetarRepository _metarRepository;
    private string? _previousMetar;

    public MockNetworkConnection(AtisStation station, IMetarRepository metarRepository)
    {
        this._metarRepository = metarRepository;

        this.Station = station;
        this.Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType)
        };

        MessageBus.Current.Listen<MetarReceived>().Subscribe(
            evt =>
            {
                if (evt.Metar.Icao == station.Identifier)
                {
                    var isNewMetar = !string.IsNullOrEmpty(this._previousMetar) &&
                                     evt.Metar.RawMetar?.Trim() != this._previousMetar?.Trim();
                    if (this._previousMetar != evt.Metar.RawMetar)
                    {
                        this.MetarResponseReceived?.Invoke(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                        this._previousMetar = evt.Metar.RawMetar;
                    }
                }
            });

        MessageBus.Current.Listen<SessionEnded>().Subscribe(_ => { this.Disconnect(); });
    }

    public AtisStation Station { get; }

    public event EventHandler? NetworkConnected = delegate { };

    public event EventHandler? NetworkDisconnected = delegate { };

    public event EventHandler? NetworkConnectionFailed = delegate { };

    public event EventHandler<MetarResponseReceived>? MetarResponseReceived = delegate { };

    public event EventHandler<NetworkErrorReceived>? NetworkErrorReceived = delegate { };

    public event EventHandler<KillRequestReceived>? KillRequestReceived = delegate { };

    public event EventHandler<ClientEventArgs<string>>? ChangeServerReceived = delegate { };

    public string Callsign { get; }

    public bool IsConnected { get; private set; }

    public Task Connect(string? serverAddress)
    {
        this._metarRepository.GetMetar(this.Station.Identifier, true);

        this.NetworkConnected?.Invoke(this, EventArgs.Empty);
        this.IsConnected = true;

        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        if (!string.IsNullOrEmpty(this.Station.Identifier))
        {
            this._metarRepository.RemoveMetar(this.Station.Identifier);
        }

        this.NetworkDisconnected?.Invoke(this, EventArgs.Empty);
        this.IsConnected = false;

        this._previousMetar = null;
    }

    public void SendSubscriberNotification(char atisLetter)
    {
        // ignore
    }
}