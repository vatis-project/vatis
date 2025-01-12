// <copyright file="Dewpoint.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the dewpoint component of the ATIS format.
/// </summary>
public class Dewpoint : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Dewpoint"/> class.
    /// </summary>
    public Dewpoint()
    {
        Template = new Template
        {
            Text = "{dewpoint}",
            Voice = "DEWPOINT {dewpoint}",
        };
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use a plus prefix for the dewpoint.
    /// </summary>
    public bool UsePlusPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to speak the leading zero for the dewpoint.
    /// </summary>
    public bool SpeakLeadingZero { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to pronounce the leading zero for the dewpoint.
    /// This property is obsolete and should not be used. Use <see cref="SpeakLeadingZero"/> instead.
    /// </summary>
    [Obsolete("Use 'SpeakLeadingZero' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PronounceLeadingZero
    {
        get => false;
        set => SpeakLeadingZero = value;
    }

    /// <summary>
    /// Creates a new instance of <see cref="Dewpoint"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Dewpoint"/> instance that is a copy of this instance.</returns>
    public Dewpoint Clone()
    {
        return (Dewpoint)MemberwiseClone();
    }
}
