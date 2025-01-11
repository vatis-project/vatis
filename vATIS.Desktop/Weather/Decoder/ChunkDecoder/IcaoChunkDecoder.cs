// <copyright file="IcaoChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

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