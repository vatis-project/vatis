// <copyright file="MetarChunkDecoderException.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Exception;

public sealed class MetarChunkDecoderException : System.Exception
{
    public string? RemainingMetar { get; private set; }
    public string? NewRemainingMetar { get; private set; }

    public MetarChunkDecoderException(string message) : base(message)
    {

    }

    public MetarChunkDecoderException(string remainingMetar, string newRemainingMetar, string message) : base(message)
    {
        RemainingMetar = remainingMetar;
        NewRemainingMetar = newRemainingMetar;
    }

    public static class Messages
    {
        private const string BadFormatFor = @"Bad format for ";

        //CloudChunkDecoder
        public const string CloudsInformationBadFormat = BadFormatFor + @"clouds information";

        //DatetimeChunkDecoder
        public const string BadDayHourMinuteInformation =
            @"Missing or badly formatted day/hour/minute information (""ddhhmmZ"" expected)";

        public const string InvalidDayHourMinuteRanges = @"Invalid values for day/hour/minute";

        //IcaoChunkDecoder
        public const string IcaoNotFound = @"Station ICAO code not found (4 char expected)";

        //PressureChunkDecoder
        public const string AtmosphericPressureNotFound = @"Atmospheric pressure not found";

        //ReportStatusChunkDecoder
        public const string InvalidReportStatus =
            @"Invalid report status, expecting AUTO, NIL, or any other 3 letter word";

        public const string NoInformationExpectedAfterNilStatus = @"No information expected after NIL status";

        //RunwayVisualRangeChunkDecoder
        public const string InvalidRunwayQfuRunwayVisualRangeInformation =
            @"Invalid runway QFU runway visual range information";

        //SurfaceWindChunkDecoder
        public const string SurfaceWindInformationBadFormat = BadFormatFor + @"surface wind information";

        public const string NoSurfaceWindInformationMeasured = @"No information measured for surface wind";
        public const string InvalidWindDirectionInterval = @"Wind direction should be in [0,360]";

        public const string InvalidWindDirectionVariationsInterval =
            @"Wind direction variations should be in [0,360]";

        //VisibilityChunkDecoder
        public const string ForVisibilityInformationBadFormat = BadFormatFor + @"visibility information";

        //WindShearChunkDecoder
        public const string InvalidRunwayQfuRunwaVisualRangeInformation =
            @"Invalid runway QFU runway visual range information";
    }
}
