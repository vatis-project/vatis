// <copyright file="BaseNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an abstract base class for ATIS nodes, providing functionality to parse ATIS data.
/// </summary>
/// <typeparam name="T">The type of the node's value.</typeparam>'
public abstract class BaseNode<T>
{
    /// <summary>
    /// Gets the ATIS station.
    /// </summary>
    public AtisStation? Station { get; init; }

    /// <summary>
    /// Gets or sets the voice ATIS text.
    /// </summary>
    public string? VoiceAtis { get; protected set; }

    /// <summary>
    /// Gets or sets the text ATIS text.
    /// </summary>
    public string? TextAtis { get; protected set; }

    /// <summary>
    /// Parses the specified METAR.
    /// </summary>
    /// <param name="metar">The decoded metar.</param>
    public abstract void Parse(DecodedMetar metar);

    /// <summary>
    /// Parses voice variables for the specified node.
    /// </summary>
    /// <param name="node">The node to parse.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>The parsed voice string response.</returns>
    public abstract string ParseVoiceVariables(T node, string? format);

    /// <summary>
    /// Parses text variables for the specified node.
    /// </summary>
    /// <param name="node">The node to parse.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>The parsed text string response.</returns>
    public abstract string ParseTextVariables(T node, string? format);
}
