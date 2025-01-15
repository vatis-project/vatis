// <copyright file="UndeterminedLayer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents an undetermined layer with text and voice components.
/// </summary>
public class UndeterminedLayer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UndeterminedLayer"/> class.
    /// </summary>
    /// <param name="text">The text component of the undetermined layer.</param>
    /// <param name="voice">The voice component of the undetermined layer.</param>
    public UndeterminedLayer(string text, string voice)
    {
        Text = text;
        Voice = voice;
    }

    /// <summary>
    /// Gets or sets the text component of the undetermined layer.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the voice component of the undetermined layer.
    /// </summary>
    public string Voice { get; set; }
}
