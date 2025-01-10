// <copyright file="PostUserRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents the data transfer object for user authentication request.
/// </summary>
public class PostUserRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostUserRequestDto"/> class.
    /// </summary>
    /// <param name="username">The username associated with the authentication request.</param>
    /// <param name="password">The password for the provided username.</param>
    /// <param name="client">The client identification information.</param>
    public PostUserRequestDto(string username, string password, string client)
    {
        this.Username = username;
        this.Password = password;
        this.Client = client;
    }

    /// <summary>
    /// Gets or sets the username for the user.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password for the user.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the client information associated with the user.
    /// </summary>
    public string Client { get; set; }
}
