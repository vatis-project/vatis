// <copyright file="MetarChunkDecoder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;

/// <summary>
/// Initializes a new instance of the <see cref="MetarChunkDecoder"/> class.
/// </summary>
public abstract class MetarChunkDecoder : IMetarChunkDecoder
{
    /// <summary>
    /// Consume one chunk blindly, without looking for the specific pattern (only whitespace).
    /// </summary>
    /// <param name="remainingMetar">The remaining portion of the METAR string to process.</param>
    /// <returns>A string with the first word removed from the remaining METAR string.</returns>
    public static string ConsumeOneChunk(string remainingMetar)
    {
        var nextSpace = remainingMetar.IndexOf(' ');
        return nextSpace > 0 ? remainingMetar[(nextSpace + 1)..] : remainingMetar;
    }

    /// <inheritdoc/>
    public abstract string GetRegex();

    /// <inheritdoc/>
    public abstract Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false);

    /// <summary>
    /// Consumes a portion of the METAR string based on the regular expression defined in the implementation
    /// of the <see cref="MetarChunkDecoder"/> class.
    /// </summary>
    /// <param name="remainingMetar">The remaining portion of the METAR string to be consumed.</param>
    /// <returns>A key-value pair where the key is the new remaining METAR string after consumption
    /// and the value is a list of matched regex groups.</returns>
    protected KeyValuePair<string, List<Group>> Consume(string remainingMetar)
    {
        var chunkRegex = new Regex(this.GetRegex(), RegexOptions.None, TimeSpan.FromMilliseconds(500));

        // try to match chunk's regexp on remaining metar
        var groups = chunkRegex.Match(remainingMetar).Groups.Cast<Group>().ToList();

        // consume what has been previously found with the same regexp
        var newRemainingMetar = chunkRegex.Replace(remainingMetar, string.Empty);

        return new KeyValuePair<string, List<Group>>(newRemainingMetar, groups);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetResults"/> method, processing remaining METAR and result.
    /// </summary>
    /// <param name="newRemainingMetar">The updated remaining portion of the METAR string after processing.</param>
    /// <param name="result">The dictionary containing processed results from the METAR string.</param>
    /// <returns>A dictionary containing the processed results and the updated remaining METAR string.</returns>
    protected Dictionary<string, object> GetResults(string newRemainingMetar, Dictionary<string, object?> result)
    {
        // return result + remaining metar
        return new Dictionary<string, object>
        {
            { MetarDecoder.ResultKey, result },
            { MetarDecoder.RemainingMetarKey, newRemainingMetar },
        };
    }
}
