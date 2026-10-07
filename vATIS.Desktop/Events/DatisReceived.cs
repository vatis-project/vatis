// <copyright file="DatisReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when D-ATIS data has been fetched and processed for a station.
/// </summary>
/// <param name="Result">The processed D-ATIS result.</param>
public record DatisReceived(DatisResult Result) : IEvent;
