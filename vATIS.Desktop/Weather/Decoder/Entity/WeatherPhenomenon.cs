using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// WeatherPhenomenon
/// </summary>
public sealed class WeatherPhenomenon
{
    private readonly List<string> mTypes = [];

    /// <summary>
    /// Intensity/proximity of the phenomenon + / - / VC (=vicinity)
    /// </summary>
    public string? IntensityProximity { get; set; }

    /// <summary>
    /// Characteristics of the phenomenon
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// Types of phenomenon
    /// </summary>
    public ReadOnlyCollection<string> Types => new(mTypes);

    /// <summary>
    /// Raw string value
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// AddType
    /// </summary>
    /// <param name="type">type</param>
    public void AddType(string type)
    {
        mTypes.Add(type);
    }
}