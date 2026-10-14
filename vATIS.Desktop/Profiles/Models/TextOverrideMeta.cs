// <copyright file="TextOverrideMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents an editable text ATIS output for a METAR code (e.g. a visibility direction or RVR tendency).
/// </summary>
public record TextOverrideMeta(string Key, string Text) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(TextOverrideMeta? other)
    {
        return other != null && Key == other.Key && Text == other.Text;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Text);
    }
}
