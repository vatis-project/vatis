// <copyright file="AtisLetterChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event raised when a station with linked letters changes its ATIS letter.
/// </summary>
/// <param name="StationId">The ID of the station whose letter changed.</param>
/// <param name="Identifier">The airport identifier of the station.</param>
/// <param name="AtisType">The ATIS type of the station.</param>
/// <param name="Letter">The new letter.</param>
/// <param name="Link">The letter link of the changed station, or null when it has none enabled.</param>
public record AtisLetterChanged(
    string StationId, string Identifier, AtisType AtisType, char Letter, AtisLetterLinkMeta? Link) : IEvent;
