// <copyright file="NumberStyle.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Defines how a numeric variable value is rendered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NumberStyle>))]
public enum NumberStyle
{
    /// <summary>
    /// Use the built-in rendering for the variable and output (digits in text, serial words in voice).
    /// </summary>
    Default,

    /// <summary>
    /// Plain digits (e.g. 1013).
    /// </summary>
    Digits,

    /// <summary>
    /// Digit-by-digit words (e.g. ONE ZERO ONE THREE).
    /// </summary>
    Serial,

    /// <summary>
    /// Group-form words (e.g. TEN THIRTEEN).
    /// </summary>
    Group,

    /// <summary>
    /// Plain digits with the decimal point removed in the text ATIS (e.g. 2997) and digit-by-digit words
    /// without the decimal in the voice ATIS (e.g. TWO NINER NINER SEVEN).
    /// </summary>
    NoDecimal,
}
