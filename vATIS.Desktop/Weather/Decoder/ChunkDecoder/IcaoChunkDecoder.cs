using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class IcaoChunkDecoder : MetarChunkDecoder
{
    private const string IcaoParameterName = "Icao";

    public override string GetRegex()
    {
        return "^([A-Z0-9]{4}) ";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        // handle the case where nothing has been found
        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.IcaoNotFound);
        }

        // retrieve found params
        result.Add(IcaoParameterName, found[1].Value);

        return GetResults(newRemainingMetar, result);
    }
}