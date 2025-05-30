// <copyright file="ConfigureAtisMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a message sent over the websocket to configure an ATIS station before connecting.
/// </summary>
public class ConfigureAtisMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "configureAtis";

    /// <summary>
    /// Gets or sets the configuration payload message.
    /// </summary>
    [JsonPropertyName("value")]
    public ConfigureAtisMessagePayload? Payload { get; set; }

    /// <summary>
    /// Represents the ATIS configuration payload.
    /// </summary>
    public class ConfigureAtisMessagePayload
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

        /// <summary>
        /// Gets or sets the preset to select.
        /// </summary>
        [JsonPropertyName("preset")]
        public string? Preset { get; set; }

        /// <summary>
        /// Gets or sets the airport conditions free-text value.
        /// </summary>
        [JsonPropertyName("airportConditionsFreeText")]
        public string? AirportConditionsFreeText { get; set; }

        /// <summary>
        /// Gets or sets the NOTAM free-text value.
        /// </summary>
        [JsonPropertyName("notamsFreeText")]
        public string? NotamsFreeText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sync the ATIS letter to the real-world letter.
        /// </summary>
        [JsonPropertyName("syncAtisLetter")]
        public bool? SyncAtisLetter { get; set; }
    }
}
