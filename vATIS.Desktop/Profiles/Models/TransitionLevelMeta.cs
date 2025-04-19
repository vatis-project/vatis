// <copyright file="TransitionLevelMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata for a transition level, including the lower level, upper level, and altitude.
/// </summary>
public record TransitionLevelMeta(int Low, int High, int Altitude) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(TransitionLevelMeta? other)
    {
        if (other != null)
        {
            return Low == other.Low && High == other.High && Altitude == other.Altitude;
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Low, High, Altitude);
    }
}
