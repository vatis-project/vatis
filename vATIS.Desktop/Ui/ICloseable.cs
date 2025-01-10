// <copyright file="ICloseable.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui;

/// <summary>
/// Defines methods to close a dialog or window with an optional result.
/// </summary>
public interface ICloseable
{
    /// <summary>
    /// Defines methods to close a dialog or window with an optional result.
    /// </summary>
    /// <param name="dialogResult">The dialog result.</param>
    void Close(object? dialogResult);

    /// <summary>
    /// Defines methods to close a dialog or window with an optional result.
    /// </summary>
    void Close();
}
