// <copyright file="AtisStationOrdinalChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is triggered when the ordinal position of an ATIS station is updated.
/// </summary>
/// <param name="Id">
/// The unique identifier of the ATIS station whose ordinal has changed.
/// </param>
/// <param name="NewOrdinal">
/// The updated ordinal position of the ATIS station after the change.
/// </param>
public record AtisStationOrdinalChanged(string Id, int NewOrdinal) : IEvent;
