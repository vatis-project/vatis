// <copyright file="MessageBox.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

/// <summary>
/// Represents a static class for displaying message boxes with various options and configurations.
/// </summary>
public static class MessageBox
{
    /// <summary>
    /// Displays a message box with the specified message text and caption.
    /// </summary>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> Show(string messageBoxText, string caption)
    {
        return await ShowCore(messageBoxText, caption, MessageBoxButton.Ok, MessageBoxIcon.None);
    }

    /// <summary>
    /// Displays a message box with the specified message text and caption.
    /// </summary>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box.</param>
    /// <param name="button">The buttons to display in the message box. This parameter is of type <see cref="MessageBoxButton"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button)
    {
        return await ShowCore(messageBoxText, caption, button, MessageBoxIcon.None);
    }

    /// <summary>
    /// Displays a message box with the specified message text and caption.
    /// </summary>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box.</param>
    /// <param name="button">The buttons to display in the message box. This parameter is of type <see cref="MessageBoxButton"/>.</param>
    /// <param name="icon">The icon to display in the message box. This parameter is of type <see cref="MessageBoxIcon"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxIcon icon)
    {
        return await ShowCore(messageBoxText, caption, button, icon);
    }

    /// <summary>
    /// Displays a message box dialog with the specified owner, message text, and caption.
    /// </summary>
    /// <param name="owner">The window that owns the message box dialog.</param>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> ShowDialog(Window owner, string messageBoxText, string caption)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, MessageBoxButton.Ok, MessageBoxIcon.None);
    }

    /// <summary>
    /// Displays a message box as a dialog window with the specified owner, message text, and caption.
    /// </summary>
    /// <param name="owner">The <see cref="Window"/> that owns this message box dialog.</param>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box dialog.</param>
    /// <param name="button">The buttons to display in the message box. This parameter is of type <see cref="MessageBoxButton"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> ShowDialog(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, button, MessageBoxIcon.None);
    }

    /// <summary>
    /// Displays a message box with the specified message text, caption, button configuration, and icon.
    /// </summary>
    /// <param name="owner">The window that owns the message box.</param>
    /// <param name="messageBoxText">The message to display in the message box.</param>
    /// <param name="caption">The title of the message box.</param>
    /// <param name="button">The buttons to display in the message box. This parameter is of type <see cref="MessageBoxButton"/>.</param>
    /// <param name="icon">The icon to display in the message box. This parameter is of type <see cref="MessageBoxIcon"/>.</param>
    /// <param name="centerWindow">A value indicating whether the dialog should be centered on the screen.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, the result of which contains the <see cref="MessageBoxResult"/> selected by the user.</returns>
    public static async Task<MessageBoxResult> ShowDialog(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxIcon icon,
        bool centerWindow = false)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, button, icon, centerWindow);
    }

    private static Task<MessageBoxResult> ShowCore(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxIcon icon)
    {
        var viewModel = new MessageBoxViewModel
        {
            Caption = caption,
            Message = messageBoxText,
            Button = button,
            Icon = icon,
        };

        var window = new MessageBoxView
        {
            DataContext = viewModel,
        };

        var tcs = new TaskCompletionSource<MessageBoxResult>();

        window.Closed += (_, _) => { tcs.TrySetResult(viewModel.Result); };
        window.Topmost = true;
        window.ShowInTaskbar = true;
        window.Show();

        return tcs.Task;
    }

    private static Task<MessageBoxResult> ShowDialogCore(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxIcon icon,
        bool centerWindow = false)
    {
        var viewModel = new MessageBoxViewModel
        {
            Caption = caption,
            Message = messageBoxText,
            Button = button,
            Icon = icon,
            Owner = owner,
            CenterWindowOnScreen = centerWindow
        };

        var window = new MessageBoxView
        {
            DataContext = viewModel,
            Topmost = owner.Topmost,
        };

        return window.ShowDialog<MessageBoxResult>(owner);
    }
}
