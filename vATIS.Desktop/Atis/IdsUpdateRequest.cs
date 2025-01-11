// <copyright file="IdsUpdateRequest.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Atis;
public class IdsUpdateRequest
{
    public required string Facility { get; set; }
    public required string Preset { get; set; }
    public required string AtisLetter { get; set; }
    public string? AirportConditions { get; set; }
    public string? Notams { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Version { get; set; }
    public string? AtisType { get; set; }
}