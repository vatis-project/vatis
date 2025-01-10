using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a weather phenomenon with its intensity, proximity, characteristics, types, and raw value.
/// </summary>
/// <seealso cref="DecodedMetar"/>
public sealed class WeatherPhenomenon
{
    private readonly List<string> types = [];

    /// <summary>
    /// Gets or sets the intensity and proximity of the weather phenomenon.
    /// </summary>
    public string? IntensityProximity { get; set; }

    /// <summary>
    /// Gets or sets the characteristics of the weather phenomenon.
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// Gets the types of the weather phenomenon.
    /// </summary>
    public ReadOnlyCollection<string> Types => new(this.types);

    /// <summary>
    /// Gets or sets the raw METAR value associated with the weather phenomenon.
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// Adds a new type to the weather phenomenon.
    /// </summary>
    /// <param name="type">The type describing a weather phenomenon.</param>
    public void AddType(string type)
    {
        this.types.Add(type);
    }
}
