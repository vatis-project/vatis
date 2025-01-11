// <copyright file="PostUserRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Voice.Dto;

/// <summary>
/// Represents the data transfer object used to post user details for voice server authentication.
/// </summary>
public class PostUserRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostUserRequestDto"/> class.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <param name="client">The client associated with the request.</param>
    public PostUserRequestDto(string username, string password, string client)
    {
        Username = username;
        Password = password;
        Client = client;
    }

    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the client associated with the request.
    /// </summary>
    public string Client { get; set; }
}
