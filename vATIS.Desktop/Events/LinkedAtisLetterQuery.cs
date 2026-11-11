// <copyright file="LinkedAtisLetterQuery.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Asks the paired departure/arrival station whether it is connected, and if so what letter it is using. The
/// event bus invokes handlers synchronously, so the answer is available as soon as publishing returns.
/// </summary>
public class LinkedAtisLetterQuery : IEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinkedAtisLetterQuery"/> class.
    /// </summary>
    /// <param name="stationId">The ID of the station asking.</param>
    /// <param name="identifier">The airport identifier of the station asking.</param>
    /// <param name="atisType">The ATIS type of the station asking.</param>
    public LinkedAtisLetterQuery(string stationId, string identifier, AtisType atisType)
    {
        StationId = stationId;
        Identifier = identifier;
        AtisType = atisType;
    }

    /// <summary>
    /// Gets the ID of the station asking.
    /// </summary>
    public string StationId { get; }

    /// <summary>
    /// Gets the airport identifier of the station asking.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the ATIS type of the station asking.
    /// </summary>
    public AtisType AtisType { get; }

    /// <summary>
    /// Gets or sets the letter of the connected partner station, or null when the partner isn't connected.
    /// </summary>
    public char? PartnerLetter { get; set; }
}
