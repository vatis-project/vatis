// <copyright file="AtisVoiceTypeChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when the voice type of ATIS station is changed.
/// </summary>
/// <param name="Id">The ID of the ATIS station that had its voice type changed.</param>
/// <param name="UseTextToSpeech">Whether the ATIS station should use text-to-speech.</param>
public record AtisVoiceTypeChanged(string Id, bool UseTextToSpeech) : IEvent;
