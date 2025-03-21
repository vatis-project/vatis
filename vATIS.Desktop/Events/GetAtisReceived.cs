// <copyright file="GetAtisReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetAllAtisReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS.</param>
/// <param name="StationId">The station ID requested.</param>
/// <param name="Station">The station requested.</param>
/// <param name="AtisType">The ATIS type requested.</param>
/// <remarks>If both <see cref="StationId"/> and <see cref="Station"/> are null, then every station is requested.</remarks>
public record GetAtisReceived(ClientMetadata Session, string? StationId, string? Station, AtisType? AtisType = AtisType.Combined) : IEvent;
