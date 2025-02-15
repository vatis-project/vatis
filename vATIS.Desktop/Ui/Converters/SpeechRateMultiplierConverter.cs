using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Converts a numeric speech rate (e.g., 0.5) to a formatted string (e.g., "0.5x").
/// </summary>
public class SpeechRateMultiplierConverter : IValueConverter
{
    /// <summary>
    /// Converts a double value representing the speech rate to a formatted string (e.g., "0.5x").
    /// </summary>
    /// <param name="value">The value to convert (expected to be a double representing the speech rate).</param>
    /// <param name="targetType">The target type (not used in this implementation).</param>
    /// <param name="parameter">Any optional parameters (not used in this implementation).</param>
    /// <param name="culture">The culture info (not used in this implementation).</param>
    /// <returns>A string formatted as the speech rate with an 'x' suffix (e.g., "1.0x").</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double rate)
        {
            return $"{rate}x"; // Format as 0.5x, 1.0x, etc.
        }

        return value;
    }

    /// <summary>
    /// Converts a formatted string (e.g., "0.5x") back to a double value representing the speech rate.
    /// </summary>
    /// <param name="value">The value to convert (expected to be a string formatted as "0.5x").</param>
    /// <param name="targetType">The target type (expected to be a double).</param>
    /// <param name="parameter">Any optional parameters (not used in this implementation).</param>
    /// <param name="culture">The culture info (not used in this implementation).</param>
    /// <returns>A double representing the speech rate (e.g., 0.5).</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If converting back (e.g., from "0.5x" to 0.5), remove the "x" and parse the number.
        if (value is string rateString && double.TryParse(rateString.Replace("x", ""), out var result))
        {
            return result;
        }

        return value;
    }
}
