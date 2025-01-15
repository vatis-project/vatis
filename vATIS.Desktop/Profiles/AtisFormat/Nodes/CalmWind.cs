// <copyright file="CalmWind.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the calm wind component of the ATIS format.
/// </summary>
public class CalmWind : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalmWind"/> class.
    /// </summary>
    public CalmWind()
    {
        Template = new Template { Text = "{wind}", Voice = "WIND CALM", };
    }

    /// <summary>
    /// Gets or sets the calm wind speed.
    /// </summary>
    public int CalmWindSpeed { get; set; } = 2;

    /// <summary>
    /// Creates a new instance of <see cref="CalmWind"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="CalmWind"/> instance that is a copy of this instance.</returns>
    public CalmWind Clone()
    {
        return (CalmWind)MemberwiseClone();
    }
}
