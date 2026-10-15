// <copyright file="RunwayConditionTermMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents the editable wording of a runway condition term.
/// </summary>
public record RunwayConditionTermMeta(string Key, string Description, string Spoken, string Text) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(RunwayConditionTermMeta? other)
    {
        return other != null && Key == other.Key && Spoken == other.Spoken && Text == other.Text;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Spoken, Text);
    }
}
