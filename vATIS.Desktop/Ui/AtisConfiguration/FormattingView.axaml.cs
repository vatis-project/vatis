// <copyright file="FormattingView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents a view that manages the user interface for configuring ATIS format.
/// </summary>
public partial class FormattingView : UserControl
{
    private IDisposable? _tabVisibilitySubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingView"/> class.
    /// </summary>
    public FormattingView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _tabVisibilitySubscription = Visual.IsVisibleProperty.Changed.AddClassHandler<TabItem>(
            (_, e) => OnVisibilityChanged(e));
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _tabVisibilitySubscription?.Dispose();
        _tabVisibilitySubscription = null;
        base.OnDetachedFromVisualTree(e);
    }

    // When a station change hides the selected tab (e.g. an ICAO-only tab on an FAA station),
    // fall back to the first visible tab so the hidden page is not left showing.
    private void OnVisibilityChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is not TabItem { IsSelected: true, IsVisible: false } tab ||
            tab.FindAncestorOfType<TabControl>() is not { } tabControl ||
            !this.IsVisualAncestorOf(tabControl))
        {
            return;
        }

        var first = tabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.IsVisible);
        if (first != null)
        {
            tabControl.SelectedItem = first;
        }
    }
}
