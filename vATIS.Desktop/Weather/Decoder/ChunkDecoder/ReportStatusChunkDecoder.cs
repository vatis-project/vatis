// <copyright file="ReportStatusChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Provides functionality to decode the report status section of a METAR string.
/// This class extends <see cref="MetarChunkDecoder"/> and implements report status parsing logic.
/// </summary>
public sealed class ReportStatusChunkDecoder : MetarChunkDecoder
{
    private const string StatusParameterName = "Status";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return "^([A-Z]+) ";
    }

    /// <inheritdoc/>
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
                throw new MetarChunkDecoderException(
                    remainingMetar,
                    newRemainingMetar,
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
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.NoInformationExpectedAfterNilStatus);
        }

        return GetResults(newRemainingMetar, result);
    }
}
