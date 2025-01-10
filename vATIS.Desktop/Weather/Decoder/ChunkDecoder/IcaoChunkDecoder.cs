using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Provides functionality to decode ICAO chunks within a METAR string.
/// </summary>
/// <remarks>
/// The <see cref="IcaoChunkDecoder"/> class is responsible for decoding and extracting information
/// related to ICAO codes from a METAR string. It derives from <see cref="MetarChunkDecoder"/> and
/// implements the required logic to parse and identify ICAO codes.
/// </remarks>
public sealed class IcaoChunkDecoder : MetarChunkDecoder
{
    private const string IcaoParameterName = "Icao";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return "^([A-Z0-9]{4}) ";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = this.Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        // handle the case where nothing has been found
        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.IcaoNotFound);
        }

        // retrieve found params
        result.Add(IcaoParameterName, found[1].Value);

        return this.GetResults(newRemainingMetar, result);
    }
}
