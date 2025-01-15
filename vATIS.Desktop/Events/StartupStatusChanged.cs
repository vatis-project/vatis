// <copyright file="StartupStatusChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when the startup status is changed.
/// </summary>
/// <param name="Status">The new status.</param>
public record StartupStatusChanged(string Status) : IEvent;
