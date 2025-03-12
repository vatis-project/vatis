// <copyright file="ConnectAtisMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a message sent over the websocket to connect an ATIS.
/// </summary>
public class ConnectAtisMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "connectAtis";

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("value")]
    public ConnectMessagePayload? Payload { get; set; }

    /// <summary>
    /// Represents the message payload.
    /// </summary>
    public class ConnectMessagePayload
    {
        /// <summary>
        /// Gets or sets the unique station ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the station identifier.
        /// </summary>
        [JsonPropertyName("station")]
        public string? Station { get; set; }

        /// <summary>
        /// Gets or sets the station ATIS type.
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisType>))]
        public AtisType? AtisType { get; set; }
    }
}
