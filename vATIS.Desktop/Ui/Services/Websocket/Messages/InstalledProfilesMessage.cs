// <copyright file="InstalledProfilesMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a websocket message that contains a list of installed profiles.
/// </summary>
public class InstalledProfilesMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "getProfiles";

    /// <summary>
    /// Gets or sets a list of ATIS preset names.
    /// </summary>
    [JsonPropertyName("profiles")]
    public List<ProfileEntity>? Profiles { get; set; }

    /// <summary>
    /// Represents a single profile record.
    /// </summary>
    public class ProfileEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the profile.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the profile.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
