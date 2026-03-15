// <copyright file="IDatisTextProcessor.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Processes raw D-ATIS text into structured airport conditions and NOTAMs.
/// </summary>
public interface IDatisTextProcessor
{
    /// <summary>
    /// Processes a raw D-ATIS body into a <see cref="DatisResult"/> containing separated airport conditions and NOTAMs.
    /// </summary>
    /// <param name="rawBody">The raw D-ATIS text body.</param>
    /// <param name="stationId">The unique identifier of the ATIS station.</param>
    /// <param name="atisLetter">The current ATIS letter from the API response, if available.</param>
    /// <param name="contractions">The contraction definitions configured for the station.</param>
    /// <param name="replacements">The text replacement rules configured for the station.</param>
    /// <returns>A <see cref="DatisResult"/> with processed airport conditions and NOTAMs.</returns>
    DatisResult Process(
        string rawBody,
        string stationId,
        char? atisLetter,
        List<ContractionMeta> contractions,
        List<DatisTextReplacement> replacements);
}
