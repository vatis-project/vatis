// <copyright file="ExternalGenerator.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a generator for external ATIS data with various configurable properties.
/// </summary>
public class ExternalGenerator
{
    /// <summary>
    /// Gets or sets a value indicating whether the external generator is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the URL associated with the external generator.
    /// </summary>
    [Obsolete("Use TextUrl and VoiceUrl instead.")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the URL for text ATIS generation.
    /// </summary>
    public string? TextUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL for voice ATIS generation.
    /// </summary>
    public string? VoiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the arrival runways for the external generator.
    /// </summary>
    public string? Arrival { get; set; }

    /// <summary>
    /// Gets or sets the departure runways for the external generator.
    /// </summary>
    public string? Departure { get; set; }

    /// <summary>
    /// Gets or sets the approaches in use for the external generator.
    /// </summary>
    public string? Approaches { get; set; }

    /// <summary>
    /// Gets or sets the remarks associated with the external generator.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Migrates the legacy Url property to the new Url properties.
    /// </summary>
    public void MigrateUrl()
    {
        #pragma warning disable CS0618
        if (!string.IsNullOrEmpty(Url))
        {
            TextUrl ??= Url;
            VoiceUrl ??= Url;
            Url = null;
        }
        #pragma warning restore CS0618
    }
}
