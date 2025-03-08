// <copyright file="AtisStationMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket with a list of ATIS stations.
/// </summary>
public class AtisStationMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "atisStations";

    /// <summary>
    /// Gets or sets a list of ATIS preset names.
    /// </summary>
    [JsonPropertyName("stations")]
    public List<AtisStationRecord>? Stations { get; set; }

    /// <summary>
    /// Represents an ATIS station record.
    /// </summary>
    public class AtisStationRecord
    {
        /// <summary>
        /// Gets or sets the unique identifier of the station.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the station.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the station ATIS type.
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisType>))]
        public AtisType AtisType { get; set; }
    }
}
