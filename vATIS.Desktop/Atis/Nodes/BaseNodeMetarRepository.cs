// <copyright file="BaseNodeMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an abstract base class for METAR repository nodes, providing functionality to parse METAR data and generate ATIS information.
/// </summary>
/// <typeparam name="T">The type of the node's value used in the ATIS parsing process.</typeparam>
public abstract class BaseNodeMetarRepository<T> : BaseNode<T>
{
    /// <inheritdoc cref="BaseNode{T}" />
    public abstract Task Parse(DecodedMetar metar, IMetarRepository metarRepository);
}
