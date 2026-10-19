// <copyright file="OnlineControllersUpdated.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when the set of online VATSIM controllers changes.
/// </summary>
public record OnlineControllersUpdated : IEvent;
