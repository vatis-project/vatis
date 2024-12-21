using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using static Vatsim.Vatis.Weather.Decoder.Entity.DecodedMetar;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class ReportTypeChunkDecoder : MetarChunkDecoder
{
    private const string TypeParameterName = "Type";

    public override string GetRegex()
    {
        return "((METAR|SPECI)( COR){0,1}) ";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            var type = found[1].Value switch
            {
                "METAR" => MetarType.Metar,
                "SPECI" => MetarType.Speci,
                "METAR COR" => MetarType.MetarCor,
                "SPECI COR" => MetarType.SpeciCor,
                _ => MetarType.Null
            };

            result.Add(TypeParameterName, type);
        }
        else
        {
            result.Add(TypeParameterName, MetarType.Null);
        }

        return GetResults(newRemainingMetar, result);
    }
}