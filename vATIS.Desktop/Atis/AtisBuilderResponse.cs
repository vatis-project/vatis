// <copyright file="AtisBuilderResponse.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

public record AtisBuilderResponse(string? TextAtis, string? SpokenText, byte[]? AudioBytes);