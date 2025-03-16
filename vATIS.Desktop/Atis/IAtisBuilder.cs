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
/// Represents an interface for building ATIS messages in text and voice formats.
/// </summary>
public interface IAtisBuilder
{
    /// <summary>
    /// Builds a voice ATIS message.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="decodedMetar">The decoded METAR.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="sandboxRequest">Whether the request is a sandbox request.</param>
    /// <returns>A <see cref="AtisBuilderVoiceAtisResponse"/> object representing the voice ATIS message.</returns>
    Task<AtisBuilderVoiceAtisResponse> BuildVoiceAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken, bool sandboxRequest = false);

    /// <summary>
    /// Builds a text ATIS message.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="decodedMetar">The decoded METAR.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="string"/> representing the text ATIS message.</returns>
    Task<string?> BuildTextAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the generated text ATIS from the specified URL in the ATIS preset.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The selected ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="rawMetar">The raw METAR string.</param>
    /// <returns>The text ATIS string.</returns>
    Task<string?> GetExternalTextAtis(AtisStation station, AtisPreset preset, string currentAtisLetter,
        string? rawMetar);

    /// <summary>
    /// Gets the generated voice ATIS from the specified URL in the ATIS preset.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The selected ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="rawMetar">The raw METAR string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="AtisBuilderVoiceAtisResponse"/> containing the generated voice ATIS data.</returns>
    Task<AtisBuilderVoiceAtisResponse?> GetExternalVoiceAtis(AtisStation station, AtisPreset preset,
        string currentAtisLetter, string? rawMetar, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a remote IDS with the current ATIS information.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="preset">The ATIS preset.</param>
    /// <param name="currentAtisLetter">The current ATIS letter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter, CancellationToken cancellationToken);
}
