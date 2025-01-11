// <copyright file="NodeParser.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Provides methods to parse ATIS nodes.
/// </summary>
public static class NodeParser
{
    /// <summary>
    /// Parses the specified ATIS node.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <typeparam name="TU">The type of the node's unit.</typeparam>
    /// <param name="metar">The decoded METAR.</param>
    /// <param name="station">The ATIS station.</param>
    /// <returns>The parsed result.</returns>
    public static ParsedResult Parse<T, TU>(DecodedMetar metar, AtisStation station)
        where T : BaseNode<TU>, new()
    {
        var obj = new T
        {
            Station = station,
        };
        obj.Parse(metar);

        return new ParsedResult
        {
            TextAtis = obj.TextAtis ?? string.Empty,
            VoiceAtis = $"{obj.VoiceAtis}.",
        };
    }

    /// <summary>
    /// Parses the specified ATIS node.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <typeparam name="TU">The type of the node's unit.</typeparam>
    /// <param name="metar">The decoded METAR.</param>
    /// <param name="station">The ATIS station.</param>
    /// <param name="metarRepository">The METAR repository.</param>
    /// <returns>The parsed result.</returns>
    public static async Task<ParsedResult> Parse<T, TU>(
        DecodedMetar metar,
        AtisStation station,
        IMetarRepository metarRepository)
        where T : BaseNodeMetarRepository<TU>, new()
    {
        var obj = new T
        {
            Station = station,
        };
        await obj.Parse(metar, metarRepository);

        return new ParsedResult
        {
            TextAtis = obj.TextAtis ?? string.Empty,
            VoiceAtis = $"{obj.VoiceAtis}.",
        };
    }
}
