using System;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// SurfaceWind
/// </summary>
public sealed class SurfaceWind
{
    /// <summary>
    /// Wind direction
    /// </summary>
    public Value? MeanDirection { get; set; }

    /// <summary>
    /// Wind variability (if true, direction is null)
    /// </summary>
    public bool VariableDirection { get; set; }

    /// <summary>
    /// Wind speed
    /// </summary>
    public Value? MeanSpeed { get; set; }

    /// <summary>
    /// Wind speed variation (gusts)
    /// </summary>
    public Value? SpeedVariations { get; set; }

    /// <summary>
    /// Boundaries for wind direction variation
    /// </summary>
    public Value[]? DirectionVariations { get; private set; }

    public Value.Unit SpeedUnit { get; set; } = Value.Unit.None;
    
    public string? RawValue { get; set; }

    /// <summary>
    /// SetDirectionVariations
    /// </summary>
    /// <param name="directionMax">directionMax</param>
    /// <param name="directionMin">directionMin</param>
    public void SetDirectionVariations(Value directionMax, Value directionMin)
    {
        DirectionVariations = [directionMax, directionMin];
    }
    
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
}