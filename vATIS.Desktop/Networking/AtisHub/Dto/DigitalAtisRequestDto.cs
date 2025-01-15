// <copyright file="DigitalAtisRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

/// <summary>
/// Represents a data transfer object for a digital ATIS request.
/// </summary>
public class DigitalAtisRequestDto
{
    /// <summary>
    /// Gets or sets the identifier for the ATIS request.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of ATIS being requested.
    /// </summary>
    public AtisType AtisType { get; set; } = AtisType.Combined;
}
