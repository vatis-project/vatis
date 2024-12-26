using System;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Networking;

public class MockNetworkConnection : INetworkConnection
{
    private readonly IMetarRepository mMetarRepository;
    private string? mPreviousMetar;
    
    public event EventHandler? NetworkConnected = delegate { };
    public event EventHandler? NetworkDisconnected = delegate { };
    public event EventHandler? NetworkConnectionFailed = delegate { };
    public event EventHandler<MetarResponseReceived>? MetarResponseReceived = delegate { };
    public event EventHandler<NetworkErrorReceived>? NetworkErrorReceived = delegate { };
    public event EventHandler<KillRequestReceived>? KillRequestReceived = delegate { };
    public event EventHandler<ClientEventArgs<string>>? ChangeServerReceived = delegate { };

    public MockNetworkConnection(AtisStation station, IMetarRepository metarRepository)
    {
        Station = station;
        Callsign = station.AtisType switch
        {
            AtisType.Combined => station.Identifier + "_ATIS",
            AtisType.Departure => station.Identifier + "_D_ATIS",
            AtisType.Arrival => station.Identifier + "_A_ATIS",
            _ => throw new Exception("Unknown AtisType: " + station.AtisType),
        };

        mMetarRepository = metarRepository;

        MessageBus.Current.Listen<MetarReceived>().Subscribe(evt =>
        {
            if (evt.Metar.Icao == station.Identifier)
            {
                var isNewMetar = !string.IsNullOrEmpty(mPreviousMetar) &&
                                 evt.Metar.RawMetar?.Trim() != mPreviousMetar?.Trim();
                if (mPreviousMetar != evt.Metar.RawMetar)
                {
                    MetarResponseReceived?.Invoke(this, new MetarResponseReceived(evt.Metar, isNewMetar));
                    mPreviousMetar = evt.Metar.RawMetar;
                }
            }
        });

        MessageBus.Current.Listen<SessionEnded>().Subscribe(_ => { Disconnect(); });
    }

    public string Callsign { get; }
    public bool IsConnected { get; private set; }
    public AtisStation Station { get; }

    public Task Connect(string? serverAddress)
    {
        mMetarRepository.GetMetar(Station.Identifier, monitor: true);
        
        NetworkConnected?.Invoke(this, EventArgs.Empty);
        IsConnected = true;

        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        if (!string.IsNullOrEmpty(Station.Identifier))
            mMetarRepository.RemoveMetar(Station.Identifier);

        NetworkDisconnected?.Invoke(this, EventArgs.Empty);
        IsConnected = false;

        mPreviousMetar = null;
    }

    public void SendSubscriberNotification(char atisLetter)
    {
        // ignore
    }
}