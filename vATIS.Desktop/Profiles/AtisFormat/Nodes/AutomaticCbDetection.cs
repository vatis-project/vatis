// <copyright file="AutomaticCbDetection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the automatic CB detection values.
/// </summary>
public class AutomaticCbDetection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutomaticCbDetection"/> class.
    /// </summary>
    /// <param name="text">The text ATIS value.</param>
    /// <param name="voice">The voice ATIS value.</param>
    public AutomaticCbDetection(string? text, string? voice)
    {
        Text = text;
        Voice = voice;
    }

    /// <summary>
    /// Gets or sets the text ATIS value.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the voice ATIS value.
    /// </summary>
    public string? Voice { get; set; }
}
