// <copyright file="ServerDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Hub.Dto;

/// <summary>
/// Represents a DTO for server data.
/// </summary>
public class ServerDto
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AtisHubDto"/>.
    /// </summary>
    public AtisHubDto? Dto { get; set; }

    /// <summary>
    /// Gets or sets the updated at date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
