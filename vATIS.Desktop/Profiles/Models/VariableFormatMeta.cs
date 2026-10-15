// <copyright file="VariableFormatMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents the editable text and voice rendering options for a variable.
/// </summary>
public record VariableFormatMeta(
    string Key, string TextStyle, string TextSuffix, string VoiceStyle, string VoiceSuffix) : ReactiveRecord
{
    /// <inheritdoc />
    public virtual bool Equals(VariableFormatMeta? other)
    {
        return other != null && Key == other.Key && TextStyle == other.TextStyle &&
               TextSuffix == other.TextSuffix && VoiceStyle == other.VoiceStyle && VoiceSuffix == other.VoiceSuffix;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, TextStyle, TextSuffix, VoiceStyle, VoiceSuffix);
    }
}
