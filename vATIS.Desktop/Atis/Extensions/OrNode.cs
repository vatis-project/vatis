// <copyright file="OrNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Met when either child node is met.
/// </summary>
/// <param name="left">The left operand.</param>
/// <param name="right">The right operand.</param>
internal sealed class OrNode(ExpressionNode left, ExpressionNode right) : ExpressionNode
{
    /// <inheritdoc />
    public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
    {
        return left.Evaluate(metar, atisFormat) || right.Evaluate(metar, atisFormat);
    }
}
