// <copyright file="WindowMessageManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// An <see cref="WindowMessageManager"/> that displays messages in a <see cref="Window"/>.
/// </summary>
[TemplatePart(PART_Items, typeof(Panel))]
public abstract class WindowMessageManager : TemplatedControl
{
    /// <summary>
    /// The list of notification cards.
    /// </summary>
    protected IList? Items;

    // ReSharper disable once InconsistentNaming
    private const string PART_Items = "PART_Items";

    private static readonly StyledProperty<int> s_maxItemsProperty =
        AvaloniaProperty.Register<WindowMessageManager, int>(nameof(MaxItems), 5);

    /// <summary>
    /// Initializes static members of the <see cref="WindowMessageManager"/> class.
    /// Sets default values for <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/> to <see cref="Stretch"/>.
    /// </summary>
    static WindowMessageManager()
    {
        HorizontalAlignmentProperty.OverrideDefaultValue<WindowMessageManager>(HorizontalAlignment.Stretch);
        VerticalAlignmentProperty.OverrideDefaultValue<WindowMessageManager>(VerticalAlignment.Stretch);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowMessageManager"/> class with default settings.
    /// </summary>
    protected WindowMessageManager()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowMessageManager"/> class,
    /// and adds it to the specified <see cref="VisualLayerManager"/>'s adorner layer if provided.
    /// </summary>
    /// <param name="visualLayerManager">
    /// The <see cref="VisualLayerManager"/> used to manage visual layers, or <c>null</c> to skip registration.
    /// </param>
    protected WindowMessageManager(VisualLayerManager? visualLayerManager)
        : this()
    {
        if (visualLayerManager is null) return;
        visualLayerManager.AdornerLayer.Children.Add(this);
        AdornerLayer.SetAdornedElement(this, visualLayerManager.AdornerLayer);
    }

    /// <summary>
    /// Gets the maximum number of messages visible at once.
    /// </summary>
    public int MaxItems
    {
        get => GetValue(s_maxItemsProperty);
        init => SetValue(s_maxItemsProperty, value);
    }

    /// <summary>
    /// Uninstalls the <see cref="WindowMessageManager"/> from its parent <see cref="AdornerLayer"/>, if it is currently attached.
    /// </summary>
    public void Uninstall()
    {
        if (Parent is AdornerLayer adornerLayer)
        {
            adornerLayer.Children.Remove(this);
            AdornerLayer.SetAdornedElement(this, null);
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var itemsControl = e.NameScope.Find<Panel>(PART_Items);
        Items = itemsControl?.Children;
    }

    /// <summary>
    /// Installs the <see cref="WindowMessageManager"/> within the <see cref="AdornerLayer"/>
    /// of the specified <see cref="TopLevel"/>.
    /// </summary>
    /// <param name="topLevel">The <see cref="TopLevel"/> instance where the message manager should be installed.</param>
    protected void InstallFromTopLevel(TopLevel topLevel)
    {
        topLevel.TemplateApplied += TopLevelOnTemplateApplied;
        var adorner = topLevel.FindDescendantOfType<VisualLayerManager>()?.AdornerLayer;
        if (adorner is not null)
        {
            adorner.Children.Add(this);
            AdornerLayer.SetAdornedElement(this, adorner);
        }
    }

    /// <summary>
    /// Handles the <see cref="TopLevel"/> template applied event. Uninstalls the current <see cref="WindowMessageManager"/>
    /// from its parent <see cref="AdornerLayer"/> and reinstall it when the template is reapplied.
    /// </summary>
    /// <param name="sender">The sender of the event, typically the <see cref="TopLevel"/> instance.</param>
    /// <param name="e">Event arguments containing details about the template application.</param>
    private void TopLevelOnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (Parent is AdornerLayer adornerLayer)
        {
            adornerLayer.Children.Remove(this);
            AdornerLayer.SetAdornedElement(this, null);
        }

        // Reinstall message manager on template reapplied.
        var topLevel = (TopLevel)sender!;
        topLevel.TemplateApplied -= TopLevelOnTemplateApplied;
        InstallFromTopLevel(topLevel);
    }
}
