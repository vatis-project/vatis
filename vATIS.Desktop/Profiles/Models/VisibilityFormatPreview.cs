// <copyright file="VisibilityFormatPreview.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a sample METAR visibility with how it is shown in the text ATIS and spoken in the voice ATIS.
/// </summary>
/// <param name="Metar">The visibility as reported in the METAR.</param>
/// <param name="Text">The text ATIS output.</param>
/// <param name="Voice">The voice ATIS output.</param>
public record VisibilityFormatPreview(string Metar, string Text, string Voice);
