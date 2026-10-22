// <copyright file="NotNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Negates the result of the inner node.
/// </summary>
/// <param name="inner">The node to negate.</param>
internal sealed class NotNode(ExpressionNode inner) : ExpressionNode
{
    /// <inheritdoc />
    public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
    {
        return !inner.Evaluate(metar, atisFormat);
    }
}
