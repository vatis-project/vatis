// <copyright file="IAtisBuilder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

public interface IAtisBuilder
{
    Task<AtisBuilderResponse> BuildAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken, bool sandboxRequest = false);
    Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter, CancellationToken cancellationToken);
}