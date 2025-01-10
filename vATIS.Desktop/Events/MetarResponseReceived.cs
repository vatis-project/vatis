using System;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Events;

public class MetarResponseReceived(DecodedMetar metar, bool isNewMetar) : EventArgs
{
    public DecodedMetar Metar { get; } = metar;

    public bool IsNewMetar { get; } = isNewMetar;
}