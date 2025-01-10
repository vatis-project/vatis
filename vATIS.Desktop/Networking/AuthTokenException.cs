// <copyright file="AuthTokenException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Represents an exception that is thrown when there is an issue with the authentication token.
/// </summary>
public class AuthTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthTokenException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public AuthTokenException(string message)
        : base(message)
    {
    }
}
