// <copyright file="Altimeter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the altimeter component of the ATIS format.
/// </summary>
public class Altimeter : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Altimeter"/> class.
    /// </summary>
    public Altimeter()
    {
        Template = new Template { Text = "A{altimeter} ({altimeter|text})", Voice = "ALTIMETER {altimeter}", };
    }

    /// <summary>
    /// Gets or sets a value indicating whether to pronounce the decimal in the altimeter value.
    /// </summary>
    public bool PronounceDecimal { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="Altimeter"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Altimeter"/> instance that is a copy of this instance.</returns>
    public Altimeter Clone()
    {
        return (Altimeter)MemberwiseClone();
    }
}
