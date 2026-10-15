// <copyright file="RunwayConditionTerm.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the wording of a runway condition term, with text and voice components.
/// </summary>
public class RunwayConditionTerm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunwayConditionTerm"/> class.
    /// </summary>
    public RunwayConditionTerm()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunwayConditionTerm"/> class.
    /// </summary>
    /// <param name="text">The text ATIS wording.</param>
    /// <param name="voice">The voice ATIS wording.</param>
    public RunwayConditionTerm(string text, string voice)
    {
        Text = text;
        Voice = voice;
    }

    /// <summary>
    /// Gets or sets the text ATIS wording.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voice ATIS wording.
    /// </summary>
    public string Voice { get; set; } = string.Empty;
}
