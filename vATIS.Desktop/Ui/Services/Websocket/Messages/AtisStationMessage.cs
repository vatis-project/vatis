// <copyright file="AtisStationMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a message sent over the websocket with a list of ATIS stations.
/// </summary>
public class AtisStationMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "stations";

    /// <summary>
    /// Gets or sets a list of ATIS preset names.
    /// </summary>
    [JsonPropertyName("stations")]
    public List<AtisStationRecord>? Stations { get; set; }

    /// <summary>
    /// Builds a station list message from profile stations. Stations without an id or identifier are skipped.
    /// Stations are ordered by ordinal, identifier and ATIS type, and presets by ordinal and name.
    /// </summary>
    /// <param name="stations">The profile stations, or null when no profile is open.</param>
    /// <returns>A message containing the station records; the list is empty when there are no stations.</returns>
    public static AtisStationMessage FromStations(IEnumerable<AtisStation>? stations)
    {
        var records = (from station in stations ?? []
            where !string.IsNullOrEmpty(station.Id) && !string.IsNullOrEmpty(station.Identifier)
            orderby station.Ordinal, station.Identifier, TypeRank(station.AtisType)
            select new AtisStationRecord
            {
                Id = station.Id,
                Name = station.Identifier,
                AtisType = station.AtisType,
                Presets =
                [
                    .. station.Presets
                        .OrderBy(n => n.Ordinal)
                        .ThenBy(n => n.Name)
                        .Select(n => n.Name)
                        .OfType<string>()
                ]
            }).ToList();

        return new AtisStationMessage { Stations = records };
    }

    private static int TypeRank(AtisType type)
    {
        // Matches the main window's tab ordering (the enum's declaration order puts Departure before Arrival).
        return type switch
        {
            AtisType.Combined => 0,
            AtisType.Arrival => 1,
            AtisType.Departure => 2,
            _ => 3
        };
    }

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

        /// <summary>
        /// Gets or sets a list of presets.
        /// </summary>
        [JsonPropertyName("presets")]
        public List<string>? Presets { get; set; }
    }
}
