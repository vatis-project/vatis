// <copyright file="DialogResult.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents the result of a dialog operation.
/// </summary>
public enum DialogResult
{
    /// <summary>
    /// Indicates that no result has been returned from the dialog.
    /// </summary>
    None,

    /// <summary>
    /// Indicates that the dialog was closed with a confirmation or acceptance.
    /// </summary>
    Ok,

    /// <summary>
    /// Indicates that the dialog operation was canceled.
    /// </summary>
    Cancel,
}
