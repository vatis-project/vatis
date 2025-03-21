// <copyright file="DecodedMetar.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a decoded METAR (Meteorological Aerodrome Report) containing information about weather observations.
/// </summary>
public class DecodedMetar
{
    private readonly string? _rawMetar;

    /// <summary>
    /// Decoding exceptions, if any.
    /// </summary>
    private List<MetarChunkDecoderException> _decodingExceptions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DecodedMetar"/> class.
    /// </summary>
    /// <param name="rawMetar">
    /// The raw string representation of the METAR data to be decoded.
    /// </param>
    internal DecodedMetar(string rawMetar = "")
    {
        RawMetar = rawMetar;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarStatus"/> enumeration.
    /// </summary>
    public enum MetarStatus
    {
        /// <summary>
        /// Represents a null value for the <see cref="DecodedMetar.MetarStatus"/> enumeration,
        /// which can be used as a placeholder or default state.
        /// </summary>
        Null,

        /// <summary>
        /// Represents an automatically generated METAR status in the <see cref="DecodedMetar.MetarStatus"/> enumeration,
        /// typically used to indicate that the observation was created through automated systems rather than manual input.
        /// </summary>
        Auto,

        /// <summary>
        /// Represents a NIL value for the <see cref="DecodedMetar.MetarStatus"/> enumeration,
        /// indicating that no METAR report is available or has been received.
        /// </summary>
        Nil,
    }

    /// <summary>
    /// Defines the various types of METARs.
    /// </summary>
    public enum MetarType
    {
        /// <summary>
        /// Represents a null value for the <see cref="DecodedMetar.MetarType"/> enumeration,
        /// which can be used as a placeholder or default state.
        /// </summary>
        Null,

        /// <summary>
        /// Represents a standard METAR report type in the <see cref="DecodedMetar.MetarType"/> enumeration.
        /// </summary>
        Metar,

        /// <summary>
        /// Represents a corrected METAR report type in the <see cref="DecodedMetar.MetarType"/> enumeration.
        /// This type is used for representing a METAR report that has been corrected after its initial issuance.
        /// </summary>
        MetarCor,

        /// <summary>
        /// Represents a special METAR report type (SPECI) in the <see cref="DecodedMetar.MetarType"/> enumeration.
        /// This type is used for representing non-routine weather observations required due to significant changes occurring
        /// between regular METAR reports.
        /// </summary>
        Speci,

        /// <summary>
        /// Represents a corrected special METAR report type (SPECI COR) in the <see cref="DecodedMetar.MetarType"/> enumeration.
        /// This type is used for non-routine weather observations that have been corrected after their initial issuance.
        /// </summary>
        SpeciCor,
    }

    /// <summary>
    /// Gets the raw METAR data obtained from a weather report in its original format.
    /// </summary>
    public string? RawMetar
    {
        get => _rawMetar?.Trim();
        private init => _rawMetar = value;
    }

    /// <summary>
    /// Gets decoding exceptions, if any.
    /// </summary>
    public ReadOnlyCollection<MetarChunkDecoderException> DecodingExceptions => new(_decodingExceptions);

    /// <summary>
    ///  Gets or sets the report type (METAR, METAR COR or SPECI).
    /// </summary>
    public MetarType Type { get; set; } = MetarType.Null;

    /// <summary>
    /// Gets or sets the ICAO code of the airport where the observation has been made.
    /// </summary>
    public string Icao { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the day of this observation.
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Gets or sets the time of the observation, as a string.
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hour component of the observation time.
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the minute component of the observation time.
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    /// Gets or sets the status of the decoded METAR report.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets information about the surface wind conditions.
    /// </summary>
    public SurfaceWind? SurfaceWind { get; set; }

    /// <summary>
    /// Gets or sets the visibility information.
    /// </summary>
    public Visibility? Visibility { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the CAVOK (Ceiling and Visibility OK) status is present.
    /// </summary>
    public bool Cavok { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of runway visual ranges.
    /// </summary>
    public List<RunwayVisualRange> RunwaysVisualRange { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of present weather phenomena observed in the METAR.
    /// </summary>
    public List<WeatherPhenomenon> PresentWeather { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of cloud layers.
    /// </summary>
    public List<CloudLayer> Clouds { get; set; } = [];

    /// <summary>
    /// Gets or sets the lowest cloud layer that is broken or overcast.
    /// </summary>
    public CloudLayer? Ceiling { get; set; }

    /// <summary>
    /// Gets or sets the air temperature.
    /// </summary>
    /// <seealso cref="Value"/>
    public Value? AirTemperature { get; set; }

    /// <summary>
    /// Gets or sets the dew point temperature.
    /// </summary>
    /// <seealso cref="Value"/>
    public Value? DewPointTemperature { get; set; }

    /// <summary>
    /// Gets or sets the pressure value.
    /// </summary>
    public Pressure? Pressure { get; set; }

    /// <summary>
    /// Gets or sets the recent weather phenomena.
    /// </summary>
    /// <seealso cref="WeatherPhenomenon"/>
    public WeatherPhenomenon? RecentWeather { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wind shear is present on all runways.
    /// </summary>
    public bool? WindshearAllRunways { get; set; }

    /// <summary>
    /// Gets or sets the list of runways where windshear is reported.
    /// </summary>
    public List<string>? WindshearRunways { get; set; }

    /// <summary>
    /// Gets or sets the trend forecast information.
    /// </summary>
    public TrendForecast? TrendForecast { get; set; }

    /// <summary>
    ///  Gets or sets the additional TREND forecast information.
    /// </summary>
    public TrendForecast? TrendForecastFuture { get; set; }

    /// <summary>
    /// Gets a value indicating whether the decoded METAR is valid, determined by the absence of decoding exceptions.
    /// </summary>
    public bool IsValid => DecodingExceptions.Count == 0;

    /// <summary>
    /// Resets the list of decoding exceptions to an empty state.
    /// </summary>
    public void ResetDecodingExceptions()
    {
        _decodingExceptions = new List<MetarChunkDecoderException>();
    }

    /// <summary>
    /// Adds a decoding exception to the list of exceptions encountered during METAR decoding.
    /// </summary>
    /// <param name="ex">
    /// The exception to be added, representing an issue that occurred during the decoding process.
    /// </param>
    public void AddDecodingException(MetarChunkDecoderException ex)
    {
        _decodingExceptions.Add(ex);
    }
}
