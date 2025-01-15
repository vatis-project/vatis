// <copyright file="AtisBuilderVoiceAtisResponse.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents a voice ATIS response.
/// </summary>
/// <param name="SpokenText">The spoken text.</param>
/// <param name="AudioBytes">The audio bytes.</param>
/// <returns>A new instance of the <see cref="AtisBuilderVoiceAtisResponse"/> class.</returns>
public record AtisBuilderVoiceAtisResponse(string? SpokenText, byte[]? AudioBytes);
