// <copyright file="DatetimeChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Represents a decoder that extracts and validates the datetime information from a METAR string segment.
/// </summary>
/// <remarks>
/// This decoder is used to parse the day, hour, and minute, ensuring they meet the expected ranges,
/// and formats the time information as per METAR specifications.
/// </remarks>
public sealed class DatetimeChunkDecoder : MetarChunkDecoder
{
    private const string DayParameterName = "Day";
    private const string TimeParameterName = "Time";
    private const string HourParameterName = "Hour";
    private const string MinuteParameterName = "Minute";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return "^([0-9]{2})([0-9]{2})([0-9]{2})Z ";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        // handle the case where nothing has been found
        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.BadDayHourMinuteInformation);
        }

        // retrieve found params and check them
        var day = Convert.ToInt32(found[1].Value);
        var hour = Convert.ToInt32(found[2].Value);
        var minute = Convert.ToInt32(found[3].Value);

        if (!CheckValidity(day, hour, minute))
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.InvalidDayHourMinuteRanges);
        }

        result.Add(HourParameterName, hour);
        result.Add(MinuteParameterName, minute);
        result.Add(DayParameterName, day);
        result.Add(TimeParameterName, $"{hour:00}:{minute:00} UTC");

        return GetResults(newRemainingMetar, result);
    }

    /// <summary>
    /// Checks the validity of the provided day, hour, and minute values.
    /// </summary>
    /// <param name="day">The day value to validate, ranging from 1 to 31.</param>
    /// <param name="hour">The hour value to validate, ranging from 0 to 23.</param>
    /// <param name="minute">The minute value to validate, ranging from 0 to 59.</param>
    /// <returns>
    /// <see langword="true"/> if the provided values for day, hour, and minute fall within valid ranges; otherwise, <see langword="false"/>.
    /// </returns>
    private bool CheckValidity(int day, int hour, int minute)
    {
        // check value range
        if (day is < 1 or > 31)
        {
            return false;
        }

        if (hour is < 0 or > 23)
        {
            return false;
        }

        return minute is >= 0 and <= 59;
    }
}
