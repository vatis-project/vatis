using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;

public abstract class MetarChunkDecoder : IMetarChunkDecoder
{
    /// <summary>
    /// Extract the corresponding chunk from the remaining metar.
    /// </summary>
    /// <param name="remainingMetar">matches array if any match (null if no match), + updated remaining metar</param>
    protected KeyValuePair<string, List<Group>> Consume(string remainingMetar)
    {
        var chunkRegex = new Regex(GetRegex(), RegexOptions.None, TimeSpan.FromMilliseconds(500));

        // try to match chunk's regexp on remaining metar
        var groups = chunkRegex.Match(remainingMetar).Groups.Cast<Group>().ToList();

        // consume what has been previously found with the same regexp
        var newRemainingMetar = chunkRegex.Replace(remainingMetar, string.Empty);

        return new KeyValuePair<string, List<Group>>(newRemainingMetar, groups);
    }

    /// <summary>
    /// Consume one chunk blindly, without looking for the specific pattern (only whitespace).
    /// </summary>
    /// <param name="remainingMetar"></param>
    public static string ConsumeOneChunk(string remainingMetar)
    {
        var nextSpace = remainingMetar.IndexOf(' ');
        return nextSpace > 0 ? remainingMetar[(nextSpace + 1)..] : remainingMetar;
    }
    
    protected Dictionary<string, object> GetResults(string newRemainingMetar, Dictionary<string, object?> result)
    {
        //return result + remaining metar
        return new Dictionary<string, object>()
        {
            { MetarDecoder.RESULT_KEY, result },
            { MetarDecoder.REMAINING_METAR_KEY, newRemainingMetar }
        };
    }
    
    public abstract string GetRegex();
    
    public abstract Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false);
}