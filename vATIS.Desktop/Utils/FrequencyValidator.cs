using System.Globalization;

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Provides utility methods for parsing and validating VHF radio frequency.
/// </summary>
public static class FrequencyValidator
{
    /// <summary>
    /// Tries to parse a frequency string in MHz and validate it falls within the VHF aviation range.
    /// </summary>
    /// <param name="input">The input frequency string (e.g. "128.350" or "128350").</param>
    /// <param name="hz">The parsed frequency in Hz (e.g., 128350000).</param>
    /// <param name="error">An error message if the input is invalid.</param>
    /// <returns>True if parsing and validation succeed; otherwise false.</returns>
    public static bool TryParseMHz(string input, out uint hz, out string? error)
    {
        hz = 0;
        error = null;

        if (!decimal.TryParse(input, CultureInfo.InvariantCulture, out var mhz))
        {
            error = "Invalid frequency format.";
            return false;
        }

        var parsedHz = (uint)(mhz * 1_000_000);

        if (parsedHz is < 118_000_000 or > 137_000_000)
        {
            error = "Invalid frequency format. The accepted frequency range is 118.000–137.000 MHz.";
            return false;
        }

        hz = parsedHz;
        return true;
    }
}
