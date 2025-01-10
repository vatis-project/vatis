// <copyright file="IWindowLocationService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Provides services for managing the restoration and updating of window locations.
/// </summary>
public interface IWindowLocationService
{
    /// <summary>
    /// Restores the window to its previously saved location.
    /// </summary>
    /// <param name="window">The window whose position needs to be restored. Can be null.</param>
    void Restore(Window? window);

    /// <summary>
    /// Updates the saved location of a window based on its current position.
    /// </summary>
    /// <param name="window">The window whose current position should be saved. Can be null.</param>
    void Update(Window? window);
}
