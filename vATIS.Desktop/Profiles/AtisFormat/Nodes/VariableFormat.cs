// <copyright file="VariableFormat.cs" company="Justin Shannon">
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
}

/// <summary>
/// Rendering options for a variable in a single output (text or voice).
/// </summary>
public class OutputFormat
{
    /// <summary>
    /// Gets or sets how the number is rendered.
    /// </summary>
    public NumberStyle Style { get; set; } = NumberStyle.Default;

    /// <summary>
    /// Gets or sets text appended to the rendered value exactly as written (include a leading space if needed).
    /// </summary>
    public string Suffix { get; set; } = string.Empty;
}

/// <summary>
/// Independent text and voice rendering options for a variable.
/// </summary>
public class VariableFormat
{
    /// <summary>
    /// Gets or sets the text ATIS rendering options.
    /// </summary>
    public OutputFormat Text { get; set; } = new();

    /// <summary>
    /// Gets or sets the voice ATIS rendering options.
    /// </summary>
    public OutputFormat Voice { get; set; } = new();
}
