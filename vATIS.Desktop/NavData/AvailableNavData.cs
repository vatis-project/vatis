// <copyright file="AvailableNavData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Represents the available navigation data.
/// </summary>
public class AvailableNavData
{
    /// <summary>
    /// Gets or sets the URL for the airport data.
    /// </summary>
    public required string AirportDataUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL for the navaid data.
    /// </summary>
    public required string NavaidDataUrl { get; set; }

    /// <summary>
    /// Gets or sets the serial number for the navigation data.
    /// </summary>
    public required string NavDataSerial { get; set; }
}
