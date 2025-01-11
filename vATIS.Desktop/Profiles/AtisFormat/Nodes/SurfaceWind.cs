// <copyright file="SurfaceWind.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the surface wind component of the ATIS format.
/// </summary>
public class SurfaceWind
{
    /// <summary>
    /// Gets or sets a value indicating whether to speak the leading zero for the wind speed.
    /// </summary>
    public bool SpeakLeadingZero { get; set; }

    /// <summary>
    /// Gets or sets the magnetic variation metadata.
    /// </summary>
    public MagneticVariationMeta MagneticVariation { get; set; } = new();

    /// <summary>
    /// Gets or sets the standard wind format.
    /// </summary>
    public BaseFormat Standard { get; set; } = new()
    {
        Template = new Template { Text = "{wind_dir}{wind_spd}KT", Voice = "WIND {wind_dir} AT {wind_spd}", },
    };

    /// <summary>
    /// Gets or sets the standard gust wind format.
    /// </summary>
    public BaseFormat StandardGust { get; set; } = new()
    {
        Template = new Template
        {
            Text = "{wind_dir}{wind_spd}G{wind_gust}KT",
            Voice = "WIND {wind_dir} AT {wind_spd} GUSTS {wind_gust}",
        },
    };

    /// <summary>
    /// Gets or sets the variable wind format.
    /// </summary>
    public BaseFormat Variable { get; set; } = new()
    {
        Template = new Template { Text = "VRB{wind_spd}KT", Voice = "WIND VARIABLE AT {wind_spd}", },
    };

    /// <summary>
    /// Gets or sets the variable gust wind format.
    /// </summary>
    public BaseFormat VariableGust { get; set; } = new()
    {
        Template = new Template
        {
            Text = "VRB{wind_spd}G{wind_gust}KT", Voice = "WIND VARIABLE AT {wind_spd} GUSTS {wind_gust}",
        },
    };

    /// <summary>
    /// Gets or sets the variable direction wind format.
    /// </summary>
    public BaseFormat VariableDirection { get; set; } = new()
    {
        Template = new Template
        {
            Text = "{wind_vmin}V{wind_vmax}", Voice = "WIND VARIABLE BETWEEN {wind_vmin} AND {wind_vmax}",
        },
    };

    /// <summary>
    /// Gets or sets the calm wind format.
    /// </summary>
    public CalmWind Calm { get; set; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="SurfaceWind"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="SurfaceWind"/> instance that is a copy of this instance.</returns>
    public SurfaceWind Clone()
    {
        return (SurfaceWind)MemberwiseClone();
    }
}
