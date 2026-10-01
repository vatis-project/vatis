// <copyright file="RemarkWind.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the remark wind component of the ATIS format.
/// </summary>
public class RemarkWind : BaseFormat
{
    /// <summary>
    /// Gets or sets the text template.
    /// </summary>
    public string? TextTemplate { get; set; } = "WIND {height}FT {dir}/{speed}KT";

    /// <summary>
    /// Gets or sets the voice template.
    /// </summary>
    public string? VoiceTemplate { get; set; } = "Wind {height} feet {dir} degrees {speed} knots";

    /// <summary>
    /// Creates a new instance of <see cref="RemarkWind"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RemarkWind"/> instance that is a copy of this instance.</returns>
    public RemarkWind Clone()
    {
        return (RemarkWind)MemberwiseClone();
    }
}
