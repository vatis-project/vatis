// <copyright file="ContractionsResponseMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a websocket message that returns the contractions for ATIS stations.
/// </summary>
public class ContractionsResponseMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "contractions";

    /// <summary>
    /// Gets or sets a list of stations and their contractions.
    /// </summary>
    [JsonPropertyName("stations")]
    public List<StationContractions>? Stations { get; set; }

    /// <summary>
    /// Represents an individual station and the contractions.
    /// </summary>
    public class StationContractions
    {
        /// <summary>
        /// Gets or sets the station ID.
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
        public AtisType? AtisType { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of contractions, where the key is the contraction identifier.
        /// </summary>
        [JsonPropertyName("contractions")]
        public Dictionary<string, ContractionDetail>? Contractions { get; set; }

        /// <summary>
        /// Represents an individual contraction.
        /// </summary>
        public class ContractionDetail
        {
            /// <summary>
            /// Gets or sets the text value.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            /// <summary>
            /// Gets or sets the voice value.
            /// </summary>
            [JsonPropertyName("voice")]
            public string? Voice { get; set; }
        }
    }
}
