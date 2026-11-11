// <copyright file="VoiceSectionText.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// The spoken text of the voice ATIS sections that can be recorded.
/// </summary>
/// <param name="AirportConditions">The spoken airport conditions, empty if there are none.</param>
/// <param name="Notams">The spoken NOTAMs, empty if there are none.</param>
public record VoiceSectionText(string AirportConditions, string Notams);
