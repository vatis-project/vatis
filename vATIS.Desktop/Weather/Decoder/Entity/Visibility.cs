// <copyright file="Visibility.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Visibility
/// </summary>
public sealed class Visibility
{
    /// <summary>
    /// Prevailing visibility
    /// </summary>
    public Value? PrevailingVisibility { get; set; }

    /// <summary>
    /// Minimum visibility
    /// </summary>
    public Value? MinimumVisibility { get; set; }

    /// <summary>
    /// Direction of minimum visibility
    /// </summary>
    public string? MinimumVisibilityDirection { get; set; }

    /// <summary>
    /// No Direction Visibility
    /// </summary>
    public bool IsNdv { get; set; } = false;
    
    /// <summary>
    /// Is CAVOK
    /// </summary>
    public bool IsCavok { get; set; }
    
    /// <summary>
    /// Raw string value from METAR
    /// </summary>
    public string? RawValue { get; set; }
}