using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;
using static Vatsim.Vatis.Weather.Decoder.Entity.Value;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Represents a decoder for parsing and interpreting the pressure-related chunk of a METAR string.
/// </summary>
/// <remarks>
/// This decoder specifically identifies and processes segments within a METAR string that define the atmospheric
/// pressure in either hectopascals or inches of mercury, depending on the format provided. It implements functionality
/// to extract and decode pressure data for further usage.
/// </remarks>
/// <seealso cref="MetarChunkDecoder"/>
public sealed class PressureChunkDecoder : MetarChunkDecoder
{
    private const string PressureParameterName = "Pressure";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return "^((Q|A)(////|[0-9]{4}))( )";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = this.Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
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
                _ => Unit.None,
            };
            pressure = new Pressure
            {
                RawValue = found[1].Value,
                Value = new Value(rawValue, units),
            };
        }

        result.Add(PressureParameterName, pressure);
        return this.GetResults(newRemainingMetar, result);
    }
}
