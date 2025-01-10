// <copyright file="Profile.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a user profile containing information such as name, identifier, and related stations.
/// </summary>
public class Profile
{
    private const int CurrentVersion = 1;

    /// <summary>
    /// Gets or sets the version of the profile.
    /// </summary>
    [DefaultValue(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Gets or sets the name of the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the profile.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the URL used for updating the profile.
    /// </summary>
    public string? UpdateUrl { get; set; }

    /// <summary>
    /// Gets or sets the update serial for the profile.
    /// </summary>
    public int? UpdateSerial { get; set; }

    /// <summary>
    /// Gets or sets the collection of ATIS stations associated with the profile.
    /// </summary>
    public List<AtisStation>? Stations { get; set; } = [];

    /// <summary>
    /// Gets or sets the composites for the profile.
    /// <remarks>Obsolete: Use <see cref="Stations"/> instead.</remarks>
    /// </summary>
    [Obsolete("Use 'Stations' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<AtisStation>? Composites
    {
        get => null;
        set => this.Stations = value;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.Name;
    }
}
