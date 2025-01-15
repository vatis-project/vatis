// <copyright file="WindowPosition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the position of a window based on X and Y coordinates.
/// </summary>
/// <param name="X">The horizontal position of the window.</param>
/// <param name="Y">The vertical position of the window.</param>
public record WindowPosition(int X, int Y);
