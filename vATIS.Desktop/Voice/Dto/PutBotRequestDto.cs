// <copyright file="PutBotRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents the data transfer object to update or add a bot with the necessary configuration options.
/// </summary>
public class PutBotRequestDto
{
    /// <summary>
    /// Gets or sets the collection of transceivers.
    /// </summary>
    public List<TransceiverDto>? Transceivers { get; set; }

    /// <summary>
    /// Gets or sets the interval of the audio.
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets or sets the collection of Opus-encoded audio data.
    /// </summary>
    public List<byte[]>? OpusData { get; set; }
}
