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
        public const string CLOUDS_INFORMATION_BAD_FORMAT = BadFormatFor + @"clouds information";

        //DatetimeChunkDecoder
        public const string BAD_DAY_HOUR_MINUTE_INFORMATION =
            @"Missing or badly formatted day/hour/minute information (""ddhhmmZ"" expected)";

        public const string INVALID_DAY_HOUR_MINUTE_RANGES = @"Invalid values for day/hour/minute";

        //IcaoChunkDecoder
        public const string ICAO_NOT_FOUND = @"Station ICAO code not found (4 char expected)";

        //PressureChunkDecoder
        public const string ATMOSPHERIC_PRESSURE_NOT_FOUND = @"Atmospheric pressure not found";

        //ReportStatusChunkDecoder
        public const string INVALID_REPORT_STATUS =
            @"Invalid report status, expecting AUTO, NIL, or any other 3 letter word";

        public const string NO_INFORMATION_EXPECTED_AFTER_NIL_STATUS = @"No information expected after NIL status";

        //RunwayVisualRangeChunkDecoder
        public const string INVALID_RUNWAY_QFU_RUNWAY_VISUAL_RANGE_INFORMATION =
            @"Invalid runway QFU runway visual range information";

        //SurfaceWindChunkDecoder
        public const string SURFACE_WIND_INFORMATION_BAD_FORMAT = BadFormatFor + @"surface wind information";

        public const string NO_SURFACE_WIND_INFORMATION_MEASURED = @"No information measured for surface wind";
        public const string INVALID_WIND_DIRECTION_INTERVAL = @"Wind direction should be in [0,360]";

        public const string INVALID_WIND_DIRECTION_VARIATIONS_INTERVAL =
            @"Wind direction variations should be in [0,360]";

        //VisibilityChunkDecoder
        public const string FOR_VISIBILITY_INFORMATION_BAD_FORMAT = BadFormatFor + @"visibility information";

        //WindShearChunkDecoder
        public const string INVALID_RUNWAY_QFU_RUNWA_VISUAL_RANGE_INFORMATION =
            @"Invalid runway QFU runway visual range information";
    }
}