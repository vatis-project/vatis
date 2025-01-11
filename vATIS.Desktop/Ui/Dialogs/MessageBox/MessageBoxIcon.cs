// <copyright file="MessageBoxIcon.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

/// <summary>
/// Specifies the icons that can be displayed by a message box.
/// </summary>
public enum MessageBoxIcon
{
    /// <summary>
    /// Indicates that no icon should be displayed in the message box.
    /// </summary>
    None,

    /// <summary>
    /// Indicates that an error icon should be displayed in the message box.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates that an informational icon should be displayed in the message box.
    /// </summary>
    Information,

    /// <summary>
    /// Indicates that a question icon should be displayed in the message box.
    /// </summary>
    Question,

    /// <summary>
    /// Indicates that a warning icon should be displayed in the message box.
    /// </summary>
    Warning,
}
