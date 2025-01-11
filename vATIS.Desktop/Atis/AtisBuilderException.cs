// <copyright file="AtisBuilderException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents an exception that is specific to the ATIS builder process within the VATSIM VATIS application.
/// </summary>
public class AtisBuilderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AtisBuilderException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AtisBuilderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisBuilderException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AtisBuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
