// <copyright file="VisibilityDirectionMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents an editable text and voice ATIS output for a compass direction of a directional minimum visibility.
/// </summary>
public record VisibilityDirectionMeta(string Key, string Text, string Voice) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(VisibilityDirectionMeta? other)
    {
        return other != null && Key == other.Key && Text == other.Text && Voice == other.Voice;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Text, Voice);
    }
}
