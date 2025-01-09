using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;
using static Vatsim.Vatis.Weather.Decoder.Entity.Value;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Chunk decoder for atmospheric pressure section.
/// </summary>
public sealed class PressureChunkDecoder : MetarChunkDecoder
{
    private const string PressureParameterName = "Pressure";

    public override string GetRegex()
    {
        return "^((Q|A)(////|[0-9]{4}))( )";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.AtmosphericPressureNotFound);
        }

        Pressure? pressure = null;
        if (found[3].Value != "////")
        {
            double rawValue = ToInt(found[3].Value)!.Value;
            var units = found[2].Value switch
            {
                "Q" => Unit.HectoPascal,
                "A" => Unit.MercuryInch,
                _ => Unit.None
            };
            pressure = new Pressure
            {
                Value = new Value(rawValue, units),
                RawValue = found[1].Value
            };
        }

        result.Add(PressureParameterName, pressure);
        return GetResults(newRemainingMetar, result);
    }
}