// <copyright file="AutoObservationNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that announces an automatic observation when the METAR is flagged AUTO.
/// </summary>
public class AutoObservationNode : BaseNode<AutoObservation>
{
    /// <inheritdoc />
    public override void Parse(DecodedMetar metar)
    {
        var format = Station?.AtisFormat.AutoObservation;
        if (format == null || !string.Equals(metar.Status, "AUTO", StringComparison.Ordinal))
        {
            return;
        }

        TextAtis = format.TextTemplate ?? string.Empty;
        VoiceAtis = format.VoiceTemplate ?? string.Empty;
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(AutoObservation node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(AutoObservation node, string? format) =>
        throw new NotImplementedException();
}
