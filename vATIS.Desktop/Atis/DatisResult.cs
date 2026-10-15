// <copyright file="DatisResult.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents the processed result of a D-ATIS fetch for a station.
/// </summary>
/// <param name="StationId">The unique identifier of the ATIS station.</param>
/// <param name="AirportConditions">The processed airport conditions text.</param>
/// <param name="Notams">The processed NOTAMs text.</param>
/// <param name="AtisLetter">The current ATIS letter, if available.</param>
public record DatisResult(string StationId, string AirportConditions, string Notams, char? AtisLetter);
