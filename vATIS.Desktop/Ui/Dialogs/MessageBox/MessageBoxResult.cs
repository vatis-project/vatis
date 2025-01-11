// <copyright file="MessageBoxResult.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

/// <summary>
/// Represents the result of a message box dialog.
/// </summary>
public enum MessageBoxResult
{
    /// <summary>
    /// Specifies that no result was returned from the message box dialog.
    /// </summary>
    None,

    /// <summary>
    /// Specifies that the user selected the OK option in the message box dialog.
    /// </summary>
    Ok,

    /// <summary>
    /// Specifies that the result of the message box dialog is Cancel.
    /// </summary>
    Cancel,

    /// <summary>
    /// Specifies that the result of the message box dialog is "Yes".
    /// </summary>
    Yes,

    /// <summary>
    /// Specifies that the "No" option was selected in the message box dialog.
    /// </summary>
    No,
}
