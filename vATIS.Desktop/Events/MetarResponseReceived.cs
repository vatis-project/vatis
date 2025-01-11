// <copyright file="MetarResponseReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a METAR response is received.
/// </summary>
/// <param name="metar">The decoded METAR.</param>
/// <param name="isNewMetar">Whether the METAR is new.</param>
public class MetarResponseReceived(DecodedMetar metar, bool isNewMetar) : EventArgs
{
    /// <summary>
    /// Gets the decoded METAR.
    /// </summary>
    public DecodedMetar Metar { get; } = metar;

    /// <summary>
    /// Gets a value indicating whether the METAR is new.
    /// </summary>
    public bool IsNewMetar { get; } = isNewMetar;
}
