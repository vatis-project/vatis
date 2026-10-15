// <copyright file="IWavPackService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Builds voice ATIS audio by stitching together pre-recorded WAV clips.
/// </summary>
public interface IWavPackService
{
    /// <summary>
    /// Stitches the spoken text into 48 kHz mono 16-bit PCM using the pack in the given folder.
    /// </summary>
    /// <param name="spokenText">The spoken text, as produced for text-to-speech.</param>
    /// <param name="packFolder">The folder containing manifest.json and the WAV clips.</param>
    /// <returns>The stitched PCM bytes and any tokens that had no clip.</returns>
    /// <exception cref="WavPackException">The pack is missing or invalid.</exception>
    public WavPackResult Build(string spokenText, string packFolder);

    /// <summary>
    /// Validates a pack folder and returns a human readable summary.
    /// </summary>
    /// <param name="packFolder">The pack folder.</param>
    /// <returns>The clip count and the problems found (empty when valid).</returns>
    public (int ClipCount, IReadOnlyList<string> Problems) Validate(string packFolder);
}

/// <summary>
/// The result of stitching a voice pack.
/// </summary>
/// <param name="Pcm">The PCM audio, or null if tokens were missing.</param>
/// <param name="MissingTokens">Tokens in the text that have no clip in the pack.</param>
public record WavPackResult(byte[]? Pcm, IReadOnlyList<string> MissingTokens);
