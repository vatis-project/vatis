using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public class DecodedMetar
{
    /// <summary>
    /// Report type (METAR, METAR COR or SPECI)
    /// </summary>
    public enum MetarType
    {
        Null,
        Metar,
        MetarCor,
        Speci,
        SpeciCor,
    }

    /// <summary>
    /// Metar Status
    /// </summary>
    public enum MetarStatus
    {
        Null,
        Auto,
        Nil,
    }

    private readonly string? _rawMetar;

    /// <summary>
    /// Raw METAR
    /// </summary>
    public string? RawMetar
    {
        get => _rawMetar?.Trim();
        private init => _rawMetar = value;
    }

    /// <summary>
    /// Decoding exceptions, if any
    /// </summary>
    private List<MetarChunkDecoderException> _decodingExceptions = [];

    /// <summary>
    /// If the decoded metar is invalid, get all the exceptions that occurred during decoding
    /// Note that in strict mode, only the first encountered exception will be reported as parsing stops on error
    /// Else return null;.
    /// </summary>
    public ReadOnlyCollection<MetarChunkDecoderException> DecodingExceptions => new(_decodingExceptions);

    /// <summary>
    /// Report type (METAR, METAR COR or SPECI)
    /// </summary>
    public MetarType Type { get; set; } = MetarType.Null;

    /// <summary>
    /// ICAO code of the airport where the observation has been made
    /// </summary>
    public string Icao { get; set; } = string.Empty;

    /// <summary>
    /// Day of this observation
    /// </summary>
    public int? Day { get; set; }

    /// <summary>
    /// Time of the observation, as a string
    /// </summary>
    public string Time { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int Minute { get; set; }

    /// <summary>
    /// Report status (AUTO or NIL)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Surface wind information
    /// </summary>
    public SurfaceWind? SurfaceWind { get; set; }

    /// <summary>
    /// Visibility information
    /// </summary>
    public Visibility? Visibility { get; set; }

    /// <summary>
    /// Is CAVOK
    /// </summary>
    public bool Cavok { get; set; } = false;

    /// <summary>
    /// Runway visual range information
    /// </summary>
    public List<RunwayVisualRange> RunwaysVisualRange { get; set; } = [];

    /// <summary>
    /// Present weather
    /// </summary>
    public List<WeatherPhenomenon> PresentWeather { get; set; } = [];

    /// <summary>
    /// Cloud layers information
    /// </summary>
    public List<CloudLayer> Clouds { get; set; } = [];

    /// <summary>
    /// Temperature information
    /// </summary>
    public Value? AirTemperature { get; set; }

    /// <summary>
    /// Temperature information
    /// </summary>
    public Value? DewPointTemperature { get; set; }

    /// <summary>
    /// Pressure information
    /// </summary>
    public Pressure? Pressure { get; set; }

    /// <summary>
    /// Recent weather
    /// </summary>
    public WeatherPhenomenon? RecentWeather { get; set; }

    /// <summary>
    /// Windshear runway information (which runways, or "all")
    /// </summary>
    public bool? WindshearAllRunways { get; set; }

    /// <summary>
    /// Windshear runway information (which runways, or "all")
    /// </summary>
    public List<string>? WindshearRunways { get; set; }

    /// <summary>
    /// TREND forecast
    /// </summary>
    public TrendForecast? TrendForecast { get; set; }

    internal DecodedMetar(string rawMetar = "")
    {
        RawMetar = rawMetar;
    }

    /// <summary>
    /// Reset the whole list of Decoding Exceptions
    /// </summary>
    public void ResetDecodingExceptions()
    {
        _decodingExceptions = new List<MetarChunkDecoderException>();
    }

    /// <summary>
    /// Check if the decoded metar is valid, i.e. if there was no error during decoding.
    /// </summary>
    public bool IsValid => DecodingExceptions.Count == 0;

    /// <summary>
    /// Add an exception that occured during metar decoding.
    /// </summary>
    /// <param name="ex"></param>
    public void AddDecodingException(MetarChunkDecoderException ex)
    {
        _decodingExceptions.Add(ex);
    }
}
