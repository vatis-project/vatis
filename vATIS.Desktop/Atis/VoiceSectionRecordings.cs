// <copyright file="VoiceSectionRecordings.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Controller recordings that replace the synthesized or stitched audio of individual voice ATIS sections.
/// </summary>
/// <param name="AirportConditions">Raw PCM audio for the airport conditions section, or null to synthesize it.</param>
/// <param name="Notams">Raw PCM audio for the NOTAMs section, or null to synthesize it.</param>
public record VoiceSectionRecordings(byte[]? AirportConditions, byte[]? Notams);
