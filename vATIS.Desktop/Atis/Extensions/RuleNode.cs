// <copyright file="RuleNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A single weather comparison.
/// </summary>
/// <param name="rule">The rule to evaluate.</param>
internal sealed class RuleNode(WeatherRule rule) : ExpressionNode
{
    /// <inheritdoc />
    public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
    {
        return WeatherRuleEvaluator.IsMet(rule, metar, atisFormat);
    }
}
