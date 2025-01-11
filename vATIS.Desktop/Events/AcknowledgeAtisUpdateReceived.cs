// <copyright file="AcknowledgeAtisUpdateReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the AcknowledgeAtisUpdateReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS information.</param>
/// <param name="Station">The station whose update is acknowledged. If null every station was acknowledged.</param>
/// <param name="AtisType">The ATIS Type whose update is acknowledged.</param>
public record AcknowledgeAtisUpdateReceived(ClientMetadata Session, string? Station, AtisType? AtisType = AtisType.Combined) : IEvent;
