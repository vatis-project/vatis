using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public sealed class PresentWeather
{
    private readonly List<int> mPrecipitations = [];

    /// <summary>
    /// Precipitations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Precipitations => new(mPrecipitations);

    private readonly List<int> mObscurations = [];

    /// <summary>
    /// Obscurations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Obscurations => new(mObscurations);

    private readonly List<int> mVicinities = [];

    /// <summary>
    /// Obscurations phenomenon
    /// </summary>
    public ReadOnlyCollection<int> Vicinities => new(mVicinities);

    /// <summary>
    /// AddPrecipitation
    /// </summary>
    /// <param name="precipitation">precipitation</param>
    public void AddPrecipitation(int precipitation)
    {
        mPrecipitations.Add(precipitation);
    }

    /// <summary>
    /// AddObscuration
    /// </summary>
    /// <param name="obscurationPhenomenon">obscurationPhenomenon</param>
    public void AddObscuration(int obscurationPhenomenon)
    {
        mObscurations.Add(obscurationPhenomenon);
    }

    /// <summary>
    /// AddVicinity
    /// </summary>
    /// <param name="vicinity">vicinity</param>
    public void AddVicinity(int vicinity)
    {
        mVicinities.Add(vicinity);
    }
}