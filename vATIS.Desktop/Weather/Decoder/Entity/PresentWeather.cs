using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public sealed class PresentWeather
{
    private readonly List<int> _precipitations = [];

    /// <summary>
    /// Precipitations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Precipitations => new(_precipitations);

    private readonly List<int> _obscurations = [];

    /// <summary>
    /// Obscurations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Obscurations => new(_obscurations);

    private readonly List<int> _vicinities = [];

    /// <summary>
    /// Obscurations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Vicinities => new(_vicinities);

    /// <summary>
    /// AddPrecipitation
    /// </summary>
    /// <param name="precipitation">precipitation</param>
    public void AddPrecipitation(int precipitation)
    {
        _precipitations.Add(precipitation);
    }

    /// <summary>
    /// AddObscuration
    /// </summary>
    /// <param name="obscurationPhenomenon">obscurationPhenomenon</param>
    public void AddObscuration(int obscurationPhenomenon)
    {
        _obscurations.Add(obscurationPhenomenon);
    }

    /// <summary>
    /// AddVicinity
    /// </summary>
    /// <param name="vicinity">vicinity</param>
    public void AddVicinity(int vicinity)
    {
        _vicinities.Add(vicinity);
    }
}
