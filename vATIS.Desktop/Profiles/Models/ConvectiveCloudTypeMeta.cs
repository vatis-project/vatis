// <copyright file="ConvectiveCloudTypeMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata information for a convective cloud type, including a key, its spoken value and its text value.
/// </summary>
public record ConvectiveCloudTypeMeta(string Key, string Value, string Text = "") : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(ConvectiveCloudTypeMeta? other)
    {
        if (other != null)
        {
            return Key == other.Key && Value == other.Value && Text == other.Text;
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value, Text);
    }
}
