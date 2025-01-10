// <copyright file="TransceiverDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using MessagePack;

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents a data transfer object (DTO) for a transceiver, containing properties for its
/// identification, frequency, geographical location, and height values.
/// </summary>
[MessagePackObject]
public class TransceiverDto
{
    /// <summary>
    /// Gets or sets the identifier for the transceiver.
    /// </summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>
    /// Gets or sets the communication frequency in hertz.
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
    /// Gets or sets the height above mean sea level, in meters.
    /// </summary>
    [Key(4)]
    public double HeightMslM { get; set; }

    /// <summary>
    /// Gets or sets the height above ground level in meters.
    /// </summary>
    [Key(5)]
    public double HeightAglM { get; set; }
}
