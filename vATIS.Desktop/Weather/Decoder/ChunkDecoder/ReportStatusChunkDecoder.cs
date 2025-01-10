using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class ReportStatusChunkDecoder : MetarChunkDecoder
{
    private const string StatusParameterName = "Status";

    public override string GetRegex()
    {
        return "^([A-Z]+) ";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();
        if (found.Count > 1)
        {
            var status = found[1].Value;
            if (status.Length != 3 && status != "AUTO")
            {
                throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                    MetarChunkDecoderException.Messages.InvalidReportStatus);
            }
            // retrieve found params
            result.Add(StatusParameterName, status);
        }
        else
        {
            result.Add(StatusParameterName, string.Empty);
        }

        if (result.Count > 0 && result[StatusParameterName] as string == "NIL" && newRemainingMetar.Trim().Length > 0)
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.NoInformationExpectedAfterNilStatus);
        }

        return GetResults(newRemainingMetar, result);
    }
}
