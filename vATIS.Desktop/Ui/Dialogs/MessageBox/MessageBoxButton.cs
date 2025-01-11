// <copyright file="MessageBoxButton.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

/// <summary>
/// Defines the set of buttons that can be displayed in a message box dialog.
/// </summary>
public enum MessageBoxButton
{
    /// <summary>
    /// Specifies that no buttons are displayed in the message box.
    /// </summary>
    None,

    /// <summary>
    /// Specifies that a message box with an "OK" button is displayed.
    /// </summary>
    Ok,

    /// <summary>
    /// Specifies that the message box displays "OK" and "Cancel" buttons.
    /// </summary>
    OkCancel,

    /// <summary>
    /// Specifies that the message box displays "Yes," "No," and "Cancel" buttons.
    /// </summary>
    YesNoCancel,

    /// <summary>
    /// Specifies that "Yes" and "No" buttons are displayed in the message box.
    /// </summary>
    YesNo,
}
