// <copyright file="AtisHubAtisReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when ATIS data is received from the ATIS hub.
/// </summary>
/// <param name="Dto">The ATIS data received from the ATIS hub.</param>
public record AtisHubAtisReceived(AtisHubDto Dto) : IEvent;
