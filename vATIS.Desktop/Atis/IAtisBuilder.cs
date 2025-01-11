// <copyright file="IAtisBuilder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Provides methods to build ATIS messages.
/// </summary>
public interface IAtisBuilder
{
    /// <summary>
    /// Builds an ATIS message.
    /// </summary>
    /// <param name="station">The station to build the ATIS for.</param>
    /// <param name="preset">The preset to use for the ATIS.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="decodedMetar">The decoded METAR for the station.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="sandboxRequest">Whether the request is a sandbox request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<AtisBuilderResponse> BuildAtis(
        AtisStation station,
        AtisPreset preset,
        char currentAtisLetter,
        DecodedMetar decodedMetar,
        CancellationToken cancellationToken,
        bool sandboxRequest = false);

    /// <summary>
    /// Updates the IDS for the specified station.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The associated ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter, CancellationToken cancellationToken);
}
