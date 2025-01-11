// <copyright file="AtisStationDeleted.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when an ATIS station is deleted.
/// </summary>
/// <param name="Id">The ID of the ATIS station that was deleted.</param>
public record AtisStationDeleted(string Id) : IEvent;
