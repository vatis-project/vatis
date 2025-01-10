// <copyright file="Temperature.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the temperature component of the ATIS format.
/// </summary>
public class Temperature : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Temperature"/> class.
    /// </summary>
    public Temperature()
    {
        this.Template = new Template
        {
            Text = "{temp}",
            Voice = "TEMPERATURE {temp}",
        };
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use a plus prefix for the temperature.
    /// </summary>
    public bool UsePlusPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to speak the leading zero for the temperature.
    /// </summary>
    public bool SpeakLeadingZero { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to pronounce the leading zero for the temperature.
    /// This property is obsolete and should not be used. Use <see cref="SpeakLeadingZero"/> instead.
    /// </summary>
    [Obsolete("Use 'SpeakLeadingZero' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PronounceLeadingZero
    {
        get => false;
        set => this.SpeakLeadingZero = value;
    }

    /// <summary>
    /// Creates a new instance of <see cref="Temperature"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Temperature"/> instance that is a copy of this instance.</returns>
    public Temperature Clone()
    {
        return (Temperature)this.MemberwiseClone();
    }
}
