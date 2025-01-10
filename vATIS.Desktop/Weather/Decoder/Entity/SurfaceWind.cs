using System;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents the surface wind information including speed, direction, and variations.
/// </summary>
public sealed class SurfaceWind
{
    /// <summary>
    /// Gets or sets the mean wind direction.
    /// </summary>
    /// <remarks>
    /// Represents the average direction of the wind, typically expressed in degrees. Can be null if the wind direction is variable.
    /// </remarks>
    public Value? MeanDirection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the wind direction is variable.
    /// </summary>
    /// <remarks>
    /// When set to true, it indicates that the wind direction cannot be defined as a single fixed value,
    /// typically due to significant and unpredictable variability in the wind direction.
    /// </remarks>
    public bool VariableDirection { get; set; }

    /// <summary>
    /// Gets or sets the mean wind speed.
    /// </summary>
    /// <remarks>
    /// Represents the average speed of the wind. Typically measured in various units such as knots, kilometers per hour, or meters per second. Can be null if no wind speed is observed.
    /// </remarks>
    public Value? MeanSpeed { get; set; }

    /// <summary>
    /// Gets or sets the speed variations of the wind.
    /// </summary>
    /// <remarks>
    /// Represents the variability in wind speed, typically expressed when fluctuations in speed are observed.
    /// This property may be null if no speed variations are reported.
    /// </remarks>
    public Value? SpeedVariations { get; set; }

    /// <summary>
    /// Gets the variations in wind direction.
    /// </summary>
    /// <remarks>
    /// Represents the range of directional variations in wind, typically used when the wind fluctuates between a maximum and minimum direction.
    /// The property contains an array of <see cref="Value"/>, where the first element indicates the maximum direction and the second indicates the minimum direction.
    /// Can be null if no variations in direction are specified.
    /// </remarks>
    public Value[]? DirectionVariations { get; private set; }

    /// <summary>
    /// Gets or sets the unit of measurement for wind speed.
    /// </summary>
    /// <remarks>
    /// Represents the unit used to quantify the wind speed, such as knots, meters per second, or kilometers per hour.
    /// Can be set to <see cref="Value.Unit.None" /> when no specific unit is applicable or available.
    /// </remarks>
    public Value.Unit SpeedUnit { get; set; } = Value.Unit.None;

    /// <summary>
    /// Gets or sets the raw string value representing the surface wind data.
    /// </summary>
    /// <remarks>
    /// Represents the unprocessed surface wind information as a string. This may include details such as wind direction, speed, and other relevant attributes in its raw format.
    /// </remarks>
    public string? RawValue { get; set; }

    /// <summary>
    /// Converts the specified speed value to knots.
    /// </summary>
    /// <param name="speed">
    /// The <see cref="Value"/> representing the speed to be converted.
    /// </param>
    /// <returns>
    /// An <see cref="IConvertible"/> value representing the speed in knots.
    /// </returns>
    public static IConvertible ToKts(Value speed)
    {
        return speed.ActualUnit switch
        {
            Value.Unit.Knot => speed.ActualValue,
            Value.Unit.KilometerPerHour => (int)(speed.ActualValue * 0.539957),
            Value.Unit.MeterPerSecond => (int)(speed.ActualValue * 1.94384),
            _ => speed.ActualValue,
        };
    }

    /// <summary>
    /// Converts the specified speed value to kilometers per hour (km/h) based on its unit.
    /// </summary>
    /// <param name="speed">
    /// The speed value to convert. The unit of the speed value is determined by the <see cref="Value.ActualUnit"/> property.
    /// </param>
    /// <returns>
    /// The converted speed value in kilometers per hour (km/h).
    /// </returns>
    public static IConvertible ToKph(Value speed)
    {
        return speed.ActualUnit switch
        {
            Value.Unit.Knot => (int)(speed.ActualValue * 1.852),
            Value.Unit.KilometerPerHour => speed.ActualValue,
            Value.Unit.MeterPerSecond => (int)(speed.ActualValue * 3.6),
            _ => speed.ActualValue,
        };
    }

    /// <summary>
    /// Converts the given speed value to meters per second (m/s).
    /// </summary>
    /// <param name="speed">
    /// The speed value to be converted, represented as an instance of <see cref="Value"/>.
    /// </param>
    /// <returns>
    /// A converted speed value in meters per second (m/s) as an <see cref="IConvertible"/>.
    /// </returns>
    public static IConvertible ToMps(Value speed)
    {
        return speed.ActualUnit switch
        {
            Value.Unit.Knot => (int)(speed.ActualValue * 0.514444),
            Value.Unit.KilometerPerHour => (int)(speed.ActualValue * 0.277778),
            Value.Unit.MeterPerSecond => speed.ActualValue,
            _ => speed.ActualValue,
        };
    }

    /// <summary>
    /// Sets the direction variation range for the surface wind.
    /// </summary>
    /// <param name="directionMax">
    /// The maximum direction variation value.
    /// </param>
    /// <param name="directionMin">
    /// The minimum direction variation value.
    /// </param>
    public void SetDirectionVariations(Value directionMax, Value directionMin)
    {
        this.DirectionVariations = [directionMax, directionMin];
    }
}
