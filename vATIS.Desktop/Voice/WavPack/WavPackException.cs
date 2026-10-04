// <copyright file="WavPackException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Thrown when a voice pack cannot be loaded.
/// </summary>
/// <param name="message">The error message.</param>
public class WavPackException(string message) : Exception(message);
