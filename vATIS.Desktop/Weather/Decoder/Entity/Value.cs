using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a measurable value with an associated unit.
/// </summary>
public sealed class Value
{
    /// <summary>
    /// Stores mapping dictionaries for unit conversion factors.
    /// The outer dictionary maps a <see cref="Unit"/> type as the source unit,
    /// and the inner dictionary contains mappings from the source <see cref="Unit"/>
    /// to a target <see cref="Unit"/> with their corresponding conversion factor.
    /// </summary>
    private readonly Dictionary<Unit, Dictionary<Unit, double>> conversionMaps = new()
    {
        {
            Unit.MeterPerSecond, new Dictionary<Unit, double>
            {
                { Unit.MeterPerSecond, 1f },
                { Unit.KilometerPerHour, 0.277778f },
                { Unit.Knot, 0.51444f },
            }
        },
        {
            Unit.Meter, new Dictionary<Unit, double>
            {
                { Unit.Meter, 1f },
                { Unit.Feet, 0.3048f },
                { Unit.StatuteMile, 1609.34f },
            }
        },
        {
            Unit.HectoPascal, new Dictionary<Unit, double>
            {
                { Unit.HectoPascal, 1f },
                { Unit.MercuryInch, 33.86389f },
            }
        },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class.
    /// </summary>
    /// <param name="value">The numerical value of the measurement.</param>
    /// <param name="unit">The unit of the measurement.</param>
    public Value(double value, Unit unit)
    {
        this.ActualValue = value;
        this.ActualUnit = unit;
    }

    /// <summary>
    /// Represents various units of measurement associated with values.
    /// </summary>
    public enum Unit
    {
        /// <summary>
        /// Represents a state where no unit of measurement is specified or applicable.
        /// </summary>
        /// <remarks>
        /// This value is typically used to indicate the absence of a defined unit.
        /// </remarks>
        [Description("")]
        None,

        /// <summary>
        /// Represents the temperature in degrees Celsius.
        /// </summary>
        /// <remarks>
        /// This unit is typically used to measure temperature in weather-related contexts.
        /// </remarks>
        [Description("deg C")]
        DegreeCelsius,

        /// <summary>
        /// Represents an angular measurement expressed in degrees.
        /// </summary>
        /// <remarks>
        /// This unit is commonly used for specifying directions, headings, or other angular values.
        /// </remarks>
        [Description("deg")]
        Degree,

        /// <summary>
        /// Represents a unit of speed measurement equivalent to one nautical mile per hour.
        /// </summary>
        /// <remarks>
        /// Commonly used in aviation and maritime contexts to measure wind speed or the speed of a vessel.
        /// </remarks>
        [Description("kt")]
        Knot,

        /// <summary>
        /// Represents a unit of measurement for speed in meters per second.
        /// </summary>
        /// <remarks>
        /// This value is commonly used in meteorological contexts, such as specifying wind speed.
        /// </remarks>
        [Description("m/s")]
        MeterPerSecond,

        /// <summary>
        /// Represents a unit of measurement for speed in kilometers per hour.
        /// </summary>
        /// <remarks>
        /// This value is commonly used for measuring velocity or wind speed in the metric system.
        /// </remarks>
        [Description("km/h")]
        KilometerPerHour,

        /// <summary>
        /// Represents a unit of measurement in meters.
        /// </summary>
        /// <remarks>
        /// This value is used to define lengths or distances in the metric system, specifically in meters.
        /// </remarks>
        [Description("m")]
        Meter,

        /// <summary>
        /// Represents a unit of measurement in feet.
        /// </summary>
        /// <remarks>
        /// This value is commonly used for altitude or height measurements in aviation and other contexts.
        /// </remarks>
        [Description("ft")]
        Feet,

        /// <summary>
        /// Represents a unit of measurement for distance in statute miles.
        /// </summary>
        /// <remarks>
        /// A statute mile is a unit of length commonly used in the United States.
        /// It is equivalent to 1,609.344 meters in the metric system.
        /// </remarks>
        [Description("SM")]
        StatuteMile,

        /// <summary>
        /// Represents a unit of atmospheric pressure measured in hectopascals (hPa).
        /// </summary>
        /// <remarks>
        /// Commonly used in meteorology to report pressure data such as barometric pressure.
        /// </remarks>
        [Description("hPa")]
        HectoPascal,

        /// <summary>
        /// Represents a unit of measurement based on inches of mercury, commonly used in barometric pressure readings.
        /// </summary>
        /// <remarks>
        /// This unit is denoted by "inHg" and is often utilized in aviation and meteorology for measuring atmospheric pressure.
        /// </remarks>
        [Description("inHg")]
        MercuryInch,

        /// <summary>
        /// Represents a unit of measurement that is not recognized or defined.
        /// </summary>
        /// <remarks>
        /// This value is used when a unit is present but cannot be interpreted or matched to a predefined unit.
        /// </remarks>
        [Description("N/A")]
        UnknownUnit,
    }

    /// <summary>
    /// Gets the actual numerical value associated with the measurement.
    /// This value is represented in the unit defined by the <see cref="ActualUnit"/> property.
    /// </summary>
    public double ActualValue { get; private set; }

    /// <summary>
    /// Gets the actual measurement unit associated with the value.
    /// This property represents the specific <see cref="Unit"/> used for the value context.
    /// </summary>
    public Unit ActualUnit { get; private set; }

    /// <summary>
    /// Converts a given string representation of a numerical value to an integer.
    /// </summary>
    /// <param name="value">The string representation of the numerical value, which may include prefixes or suffixes such as 'M' or 'P'.</param>
    /// <returns>
    /// An integer representation of the input string, if the conversion is successful; otherwise, <see langword="null"/>.
    /// </returns>
    public static int? ToInt(string value)
    {
        var valueNumeric = Regex.Replace(value.Replace("P", string.Empty).Replace("M", "-"), "[A-Z]", string.Empty);
        if (Regex.Match(valueNumeric, @"^[\-0-9]").Success)
        {
            return Convert.ToInt32(valueNumeric);
        }

        return null;
    }

    /// <summary>
    /// Converts the current value to the specified unit.
    /// </summary>
    /// <param name="unitTo">The unit to which the value will be converted.</param>
    /// <returns>The converted value as a floating-point number.</returns>
    public float GetConvertedValue(Unit unitTo)
    {
        var rateFrom = this.GetConversionRate(this.ActualUnit);
        var rateTo = this.GetConversionRate(unitTo);
        return (float)Math.Round(this.ActualValue * rateFrom / rateTo, 3);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.ActualValue} {this.ActualUnit}";
    }

    /// <summary>
    /// Retrieves the conversion rate for the specified unit relative to the current unit.
    /// </summary>
    /// <param name="unit">The target unit for which the conversion rate is required.</param>
    /// <returns>The conversion rate between the current unit and the specified unit.</returns>
    /// <exception cref="ArgumentException">Thrown if the conversion rate between the specified unit and the current unit is not defined.</exception>
    private double GetConversionRate(Unit unit)
    {
        var conversionMap = this.GetConversionMap();
        if (!conversionMap.Value.TryGetValue(unit, out var rate))
        {
            throw new ArgumentException(
                $"""Conversion rate between "{unit}" and "{this.ActualUnit}" is not defined.""");
        }

        return rate;
    }

    /// <summary>
    /// Retrieves the conversion map that contains the target unit of measurement.
    /// </summary>
    /// <returns>
    /// A <see cref="KeyValuePair{TKey, TValue}"/> where the key is the source <see cref="Unit"/>
    /// and the value is a dictionary containing target <see cref="Unit"/> as the key and the conversion factor as the value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no appropriate conversion map is found for the actual unit.
    /// </exception>
    private KeyValuePair<Unit, Dictionary<Unit, double>> GetConversionMap()
    {
        foreach (var conversionMap in this.conversionMaps)
        {
            if (conversionMap.Value.ContainsKey(this.ActualUnit))
            {
                return conversionMap;
            }
        }

        throw new ArgumentException("Trying to convert unsupported values");
    }
}
