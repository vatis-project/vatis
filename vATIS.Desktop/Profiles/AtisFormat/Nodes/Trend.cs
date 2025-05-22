// <copyright file="Trend.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the TREND component of the ATIS format.
/// </summary>
public class Trend : BaseFormat
{
    /// <summary>
    /// Gets or sets the NOSIG text value.
    /// </summary>
    public string? NosigText { get; set; } = "NOSIG";

    /// <summary>
    /// Gets or sets the NOSIG voice value.
    /// </summary>
    public string? NosigVoice { get; set; } = "No Significant Changes";

    /// <summary>
    /// Gets or sets the BECMG text value.
    /// </summary>
    public string? BecomingText { get; set; } = "BECMG";

    /// <summary>
    /// Gets or sets the BECMG voice value.
    /// </summary>
    public string? BecomingVoice { get; set; } = "Becoming";

    /// <summary>
    /// Gets or sets the TEMPO text value.
    /// </summary>
    public string? TemporaryText { get; set; } = "TEMPO";

    /// <summary>
    /// Gets or sets the TEMPO voice value.
    /// </summary>
    public string? TemporaryVoice { get; set; } = "Temporary";

    /// <summary>
    /// Gets or sets the text value for when the TREND is not available.
    /// </summary>
    public string? NotAvailableText { get; set; }

    /// <summary>
    /// Gets or sets the voice value for when the TREND is not available.
    /// </summary>
    public string? NotAvailableVoice { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="Trend"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Trend"/> instance that is a copy of this instance.</returns>
    public Trend Clone()
    {
        return (Trend)MemberwiseClone();
    }
}
