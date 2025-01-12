// <copyright file="PutBotRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents the data transfer object used to add or update a bot on the voice server.
/// </summary>
public class PutBotRequestDto
{
    /// <summary>
    /// Gets or sets the list of transceivers associated with the bot request.
    /// </summary>
    public List<TransceiverDto>? Transceivers { get; set; }

    /// <summary>
    /// Gets or sets the time interval for <see cref="OpusData"/>.
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets or sets the collection of encoded Opus audio data segments.
    /// </summary>
    public List<byte[]>? OpusData { get; set; }
}
