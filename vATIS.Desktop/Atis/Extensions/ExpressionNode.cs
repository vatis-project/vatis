// <copyright file="ExpressionNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A node in a parsed weather condition expression tree.
/// </summary>
internal abstract class ExpressionNode
{
    /// <summary>
    /// Evaluates this node against the given METAR.
    /// </summary>
    /// <param name="metar">The decoded METAR, or null if none is available.</param>
    /// <param name="atisFormat">The ATIS format used to interpret units.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public abstract bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat);
}
