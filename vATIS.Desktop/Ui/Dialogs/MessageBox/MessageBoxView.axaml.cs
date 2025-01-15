// <copyright file="MessageBoxView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

/// <summary>
/// Represents a view for displaying a message box dialog window.
/// </summary>
public partial class MessageBoxView : Window, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxView"/> class.
    /// </summary>
    public MessageBoxView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the window is closed.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MessageBoxViewModel vm)
        {
            vm.Dispose();
        }
    }

    /// <summary>
    /// Called when the window is opened.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        Dispatcher.UIThread.InvokeAsync(CenterWindow);
    }

    private void CenterWindow()
    {
        if (DataContext is MessageBoxViewModel { Owner: not null } viewModel)
        {
            var owner = viewModel.Owner;
            var ownerPosition = owner.Position;

            Position = new PixelPoint(
                (int)(ownerPosition.X + ((owner.Width - Width) / 2)),
                (int)(ownerPosition.Y + ((owner.Height - Height) / 2)));
        }
    }
}
