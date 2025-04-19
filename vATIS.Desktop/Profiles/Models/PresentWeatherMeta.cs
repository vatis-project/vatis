// <copyright file="PresentWeatherMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata information for current weather conditions in a structured format.
/// </summary>
public record PresentWeatherMeta(string Key, string Text, string Spoken) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(PresentWeatherMeta? other)
    {
        if (other != null)
        {
            return Key == other.Key && Text == other.Text && Spoken == other.Spoken;
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Text, Spoken);
    }
}
