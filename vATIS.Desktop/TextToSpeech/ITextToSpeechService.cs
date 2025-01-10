// <copyright file="ITextToSpeechService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.TextToSpeech;

/// <summary>
/// Provides functionality for text-to-speech conversion and related operations.
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Gets the available list of voices with their metadata for text-to-speech conversion.
    /// </summary>
    List<VoiceMetaData> VoiceList { get; }

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task Initialize();

    /// <summary>
    /// Requests audio synthesis for the specified text and ATIS station data.
    /// </summary>
    /// <param name="text">The text to be synthesized into audio.</param>
    /// <param name="station">The ATIS station metadata associated with the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation, containing the synthesized audio as a byte array or null if the request failed.</returns>
    Task<byte[]?> RequestAudio(string text, AtisStation station, CancellationToken cancellationToken);
}
