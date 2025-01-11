// <copyright file="MetarReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a METAR is received.
/// </summary>
/// <param name="Metar">The decoded METAR.</param>
public record MetarReceived(DecodedMetar Metar) : IEvent;
