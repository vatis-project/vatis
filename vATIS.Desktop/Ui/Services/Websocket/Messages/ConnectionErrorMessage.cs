// <copyright file="ConnectionErrorMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a message sent over the websocket when a station fails to connect to the network, or the network
/// refuses or ends its connection.
/// </summary>
public class ConnectionErrorMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "connectionError";

    /// <summary>
    /// Gets or sets the error information.
    /// </summary>
    [JsonPropertyName("value")]
    public ConnectionErrorValue? Value { get; set; }

    /// <summary>
    /// Builds a connection error message for a station.
    /// </summary>
    /// <param name="station">The station the error is for.</param>
    /// <param name="reason">Why the connection failed.</param>
    /// <param name="message">The error as shown to the user.</param>
    /// <returns>The message.</returns>
    public static ConnectionErrorMessage FromStation(AtisStation station, ConnectionErrorReason reason,
        string message)
    {
        return new ConnectionErrorMessage
        {
            Value = new ConnectionErrorValue
            {
                Id = station.Id,
                Station = station.Identifier,
                AtisType = station.AtisType,
                Reason = reason,
                Message = message
            }
        };
    }

    /// <summary>
    /// Represents the value of a connection error message.
    /// </summary>
    public class ConnectionErrorValue
    {
        /// <summary>
        /// Gets or sets the unique ID of the station.
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
        public AtisType AtisType { get; set; }

        /// <summary>
        /// Gets or sets why the connection failed.
        /// </summary>
        [JsonPropertyName("reason")]
        [JsonConverter(typeof(JsonStringEnumConverter<ConnectionErrorReason>))]
        public ConnectionErrorReason Reason { get; set; }

        /// <summary>
        /// Gets or sets the error as shown to the user.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
