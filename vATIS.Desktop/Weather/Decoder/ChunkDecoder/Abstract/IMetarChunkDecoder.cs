// <copyright file="IMetarChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;

public interface IMetarChunkDecoder
{
    /// <summary>
    /// Get the regular expression that will be used by chunk decoder
    /// Each chunk decoder must declare its own.
    /// </summary>
    string GetRegex();

    /// <summary>
    /// Decode the chunk targeted by the chunk decoder and returns the
    /// decoded information and the remaining metar without this chunk.
    /// </summary>
    /// <param name="remainingMetar"></param>
    /// <param name="withCavok"></param>
    Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false);
}