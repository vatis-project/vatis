// <copyright file="MetarChunkDecoderException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Exception;

/// <summary>
/// Represents an exception that is thrown when decoding a METAR chunk encounters an error.
/// </summary>
public sealed class MetarChunkDecoderException : System.Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetarChunkDecoderException"/> class.
    /// </summary>
    /// <param name="message">The exception message that describes the error.</param>
    public MetarChunkDecoderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarChunkDecoderException"/> class.
    /// </summary>
    /// <param name="remainingMetar">The remaining METAR string.</param>
    /// <param name="newRemainingMetar">The new remaining METAR string.</param>
    /// <param name="message">The exception message that describes the error.</param>
    public MetarChunkDecoderException(string remainingMetar, string newRemainingMetar, string message)
        : base(message)
    {
        this.RemainingMetar = remainingMetar;
        this.NewRemainingMetar = newRemainingMetar;
    }

    /// <summary>
    /// Gets the remaining portion of the METAR string
    /// after a chunk of it has been processed.
    /// </summary>
    public string? RemainingMetar { get; private set; }

    /// <summary>
    /// Gets the new remaining portion of the METAR string
    /// after an additional chunk of it has been processed.
    /// </summary>
    public string? NewRemainingMetar { get; private set; }

    /// <summary>
    /// Contains predefined error message constants used by <see cref="MetarChunkDecoderException"/>.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// Predefined error message indicating a bad format for clouds information.
        /// Used by <see cref="MetarChunkDecoderException"/> when decoding cloud-related data.
        /// </summary>
        public const string CloudsInformationBadFormat = BadFormatFor + @"clouds information";

        /// <summary>
        /// Missing or badly formatted day/hour/minute information ("ddhhmmZ" expected).
        /// Used as an error message constant in <see cref="MetarChunkDecoderException.Messages"/>.
        /// </summary>
        public const string BadDayHourMinuteInformation =
            @"Missing or badly formatted day/hour/minute information (""ddhhmmZ"" expected)";

        /// <summary>
        /// Represents an error message indicating invalid values
        /// for day, hour, or minute in the date/time information.
        /// </summary>
        public const string InvalidDayHourMinuteRanges = @"Invalid values for day/hour/minute";

        /// <summary>
        /// Indicates that the station ICAO code was not found.
        /// Expected format is a 4-character ICAO code.
        /// </summary>
        public const string IcaoNotFound = @"Station ICAO code not found (4 char expected)";

        /// <summary>
        /// Error message indicating that atmospheric pressure information was not found
        /// in the METAR string during decoding.
        /// </summary>
        public const string AtmosphericPressureNotFound = @"Atmospheric pressure not found";

        /// <summary>
        /// Represents an error message indicating an invalid report status,
        /// expecting values such as AUTO, NIL, or any other three-letter word.
        /// </summary>
        public const string InvalidReportStatus =
            @"Invalid report status, expecting AUTO, NIL, or any other 3 letter word";

        /// <summary>
        /// Indicates that no additional information should follow a NIL status in the METAR report.
        /// </summary>
        public const string NoInformationExpectedAfterNilStatus = @"No information expected after NIL status";

        /// <summary>
        /// Represents an error message indicating invalid
        /// runway QFU runway visual range information.
        /// </summary>
        public const string InvalidRunwayQfuRunwayVisualRangeInformation =
            @"Invalid runway QFU runway visual range information";

        /// <summary>
        /// Error message indicating that there is a bad format in surface wind information.
        /// </summary>
        public const string SurfaceWindInformationBadFormat = BadFormatFor + @"surface wind information";

        /// <summary>
        /// Message indicating that no information was measured for surface wind.
        /// </summary>
        public const string NoSurfaceWindInformationMeasured = @"No information measured for surface wind";

        /// <summary>
        /// Represents the error message indicating that
        /// the wind direction value is outside the valid range [0,360].
        /// </summary>
        public const string InvalidWindDirectionInterval = @"Wind direction should be in [0,360]";

        /// <summary>
        /// Indicates that wind direction variations should be within the valid range of [0,360].
        /// </summary>
        public const string InvalidWindDirectionVariationsInterval =
            @"Wind direction variations should be in [0,360]";

        /// <summary>
        /// Indicates a bad format error for visibility information in the METAR string.
        /// </summary>
        /// <see cref="Vatsim.Vatis.Weather.Decoder.Exception.MetarChunkDecoderException.Messages"/>
        /// <see cref="Vatsim.Vatis.Weather.Decoder.ChunkDecoder.VisibilityChunkDecoder"/>
        public const string ForVisibilityInformationBadFormat = BadFormatFor + @"visibility information";

        /// <summary>
        /// Represents an identifier for an invalid runway QFU (runway direction)
        /// or runway visual range information in the context of METAR data decoding.
        /// </summary>
        public const string InvalidRunwayQfuRunwaVisualRangeInformation =
            @"Invalid runway QFU runway visual range information";

        private const string BadFormatFor = @"Bad format for ";
    }
}
