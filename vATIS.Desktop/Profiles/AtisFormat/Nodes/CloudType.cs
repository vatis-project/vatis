// <copyright file="CloudType.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents a cloud type with text and voice components.
/// </summary>
public class CloudType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudType"/> class.
    /// </summary>
    /// <param name="text">The text component of the cloud type.</param>
    /// <param name="voice">The voice component of the cloud type.</param>
    public CloudType(string text, string voice)
    {
        this.Text = text;
        this.Voice = voice;
    }

    /// <summary>
    /// Gets or sets the text component of the cloud type.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the voice component of the cloud type.
    /// </summary>
    public string Voice { get; set; }

    /// <summary>
    /// Gets the type of the cloud type. This property is obsolete and should not be used.
    /// </summary>
    [JsonPropertyName("$type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Obsolete("Do not use")]
    public string? Type => null;
}
