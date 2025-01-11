// <copyright file="SoundType.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Voice.Audio;

/// <summary>
/// Represents the types of sounds that can be played through the native audio system.
/// </summary>
public enum SoundType
{
    /// <summary>
    /// Represents an error sound type used to indicate an error condition.
    /// </summary>
    Error,

    /// <summary>
    /// Represents a notification sound type used to indicate non-critical alerts or updates.
    /// </summary>
    Notification
}
