// <copyright file="TransceiverDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using MessagePack;

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents a data transfer object for ATIS bot transceivers.
/// </summary>
[MessagePackObject]
public class TransceiverDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the transceiver.
    /// </summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>
    /// Gets or sets the frequency value (in hertz) associated with the transceiver.
    /// </summary>
    [Key(1)]
    public uint Frequency { get; set; }

    /// <summary>
    /// Gets or sets the latitude in degrees.
    /// </summary>
    [Key(2)]
    public double LatDeg { get; set; }

    /// <summary>
    /// Gets or sets the longitude in degrees.
    /// </summary>
    [Key(3)]
    public double LonDeg { get; set; }

    /// <summary>
    /// Gets or sets the height of the transceiver above mean sea level, in meters.
    /// </summary>
    [Key(4)]
    public double HeightMslM { get; set; }

    /// <summary>
    /// Gets or sets the height of the transceiver above ground level, in meters.
    /// </summary>
    [Key(5)]
    public double HeightAglM { get; set; }
}
