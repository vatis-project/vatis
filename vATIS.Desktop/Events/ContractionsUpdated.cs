// <copyright file="ContractionsUpdated.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when contractions are updated.
/// </summary>
/// <param name="StationId">The ID of the station that was updated.</param>
public record ContractionsUpdated(string StationId) : IEvent;
