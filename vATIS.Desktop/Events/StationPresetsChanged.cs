// <copyright file="StationPresetsChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when station presets are changed.
/// </summary>
/// <param name="Id">The station ID.</param>
public record StationPresetsChanged(string Id) : IEvent;
