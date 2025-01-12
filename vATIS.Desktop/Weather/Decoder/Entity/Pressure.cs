// <copyright file="Pressure.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents atmospheric pressure information, including its value and raw string representation.
/// </summary>
public sealed class Pressure
{
    /// <summary>
    /// Gets or sets the pressure value.
    /// </summary>
    /// <seealso cref="Vatsim.Vatis.Weather.Decoder.Entity.Value"/>
    public Value? Value { get; set; }

    /// <summary>
    /// Gets or sets the raw string representation of the pressure value.
    /// </summary>
    /// <seealso cref="Vatsim.Vatis.Weather.Decoder.Entity.Pressure"/>
    public string? RawValue { get; set; }
}
