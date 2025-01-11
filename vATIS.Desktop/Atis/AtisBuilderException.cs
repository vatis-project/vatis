// <copyright file="AtisBuilderException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Atis;
public class AtisBuilderException : Exception
{
    public AtisBuilderException(string message) : base(message)
    {
    }

    public AtisBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}