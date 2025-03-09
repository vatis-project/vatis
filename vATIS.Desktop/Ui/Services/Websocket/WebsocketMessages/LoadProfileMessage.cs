// <copyright file="LoadProfileMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.Websocket.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket to change the active profile.
/// </summary>
public class LoadProfileMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "loadProfile";

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("value")]
    public LoadProfilePayload? Payload { get; set; }

    /// <summary>
    /// Represents the payload message.
    /// </summary>
    public class LoadProfilePayload
    {
        /// <summary>
        /// Gets or sets the profile ID.
        /// </summary>
        [JsonPropertyName("profileId")]
        public string? ProfileId { get; set; }
    }
}
