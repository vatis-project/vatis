using System.Collections.Generic;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;

/// <summary>
/// Defines the interface for a METAR chunk decoder. A METAR chunk decoder is responsible for
/// identifying and parsing specific segments or "chunks" within a METAR string.
/// </summary>
public interface IMetarChunkDecoder
{
    /// <summary>
    /// Retrieves the regular expression pattern used to match specific METAR chunks.
    /// </summary>
    /// <returns>A string representing the regular expression pattern.</returns>
    string GetRegex();

    /// <summary>
    /// Parses the given METAR string and processes the relevant data based on the implemented logic.
    /// </summary>
    /// <param name="remainingMetar">The remaining portion of the METAR string to decode.</param>
    /// <param name="withCavok">Specifies whether to decode METAR with the CAVOK field enabled.</param>
    /// <returns>A dictionary containing extracted METAR data as key-value pairs.</returns>
    Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false);
}
