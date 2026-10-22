// <copyright file="ModalToolTipFallback.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Works around tooltips not staying open in dialog windows on macOS.
/// </summary>
/// <remarks>
/// After every rendered frame Avalonia's <c>TopLevel.UpdateToolTip</c> converts the last pointer position to window
/// coordinates and hit-tests it. In a macOS dialog that lands on the wrong element, so the tooltip service decides the
/// pointer has left the tooltip's control and closes the tip, usually within a frame of it opening. The service is shared
/// by all windows, and the windows behind a modal dialog are never told the pointer left them, so they close the
/// dialog's tip too when they re-render. While a dialog is open this class clears the tooltip service of the dialog
/// and its owner windows so that render-driven update never runs; hover-driven updates are unaffected. It also opens
/// the tooltip of the control under the pointer itself after the usual show delay, unless Avalonia already did, and
/// closes it when the pointer leaves or a button is pressed.
/// </remarks>
public static class ModalToolTipFallback
{
    private static readonly FieldInfo? s_toolTipServiceField =
        typeof(TopLevel).GetField("_tooltipService", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly ConditionalWeakTable<TopLevel, object> s_patchedWindows = new();
    private static readonly Dictionary<Window, (object? Service, int Count)> s_suspendedWindows = new();
    private static bool s_registered;
    private static Control? s_current;
    private static Control? s_openedByUs;
    private static CancellationTokenSource? s_cancellation;

    /// <summary>
    /// Starts watching pointer events. Does nothing except on macOS, unless <paramref name="force"/> is set.
    /// </summary>
    /// <param name="force">True to register on any platform, for testing.</param>
    public static void Register(bool force = false)
    {
        if (s_registered || (!force && !OperatingSystem.IsMacOS()))
        {
            return;
        }

        s_registered = true;
        InputElement.PointerEnteredEvent.AddClassHandler<Control>(OnPointerEntered, RoutingStrategies.Direct);
        InputElement.PointerExitedEvent.AddClassHandler<Control>(OnPointerExited, RoutingStrategies.Direct);

        // A tip can be set while the pointer is already over the control (for example the grid cell tips).
        InputElement.PointerMovedEvent.AddClassHandler<Control>(OnPointerMoved, RoutingStrategies.Bubble);
        InputElement.PointerPressedEvent.AddClassHandler<Control>(
            (_, _) => Stop(),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private static bool IsEligible(Control control)
    {
        // Only dialogs (windows with an owner) are affected, and Avalonia's service works everywhere else.
        return ToolTip.GetTip(control) != null &&
               ToolTip.GetServiceEnabled(control) &&
               (control.IsEffectivelyEnabled || ToolTip.GetShowOnDisabled(control)) &&
               TopLevel.GetTopLevel(control) is Window { Owner: not null };
    }

    private static void DisableRenderToolTipUpdates(Control control)
    {
        if (TopLevel.GetTopLevel(control) is not Window { Owner: not null } dialog ||
            s_patchedWindows.TryGetValue(dialog, out _))
        {
            return;
        }

        s_patchedWindows.Add(dialog, new object());
        if (s_toolTipServiceField == null)
        {
            Log.Warning("Could not find TopLevel._tooltipService; tooltips in dialogs may close early on macOS.");
            return;
        }

        // The tooltip service is shared by every window, and each window re-checks it after rendering. macOS does not
        // tell the windows behind a modal dialog that the pointer left them, so they keep hit-testing a stale position
        // and close the dialog's tip. Suspend them all while the dialog is open.
        var suspended = new List<Window> { dialog };
        for (var owner = dialog.Owner as Window; owner != null; owner = owner.Owner as Window)
        {
            suspended.Add(owner);
        }

        foreach (var window in suspended)
        {
            Suspend(window);
        }

        dialog.Closed += (_, _) =>
        {
            foreach (var window in suspended)
            {
                Resume(window);
            }
        };
    }

    private static void Suspend(Window window)
    {
        try
        {
            if (s_suspendedWindows.TryGetValue(window, out var entry))
            {
                s_suspendedWindows[window] = (entry.Service, entry.Count + 1);
                return;
            }

            s_suspendedWindows[window] = (s_toolTipServiceField!.GetValue(window), 1);
            s_toolTipServiceField.SetValue(window, null);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not suspend render-driven tooltip updates for {Window}", window.GetType().Name);
        }
    }

    private static void Resume(Window window)
    {
        try
        {
            if (!s_suspendedWindows.TryGetValue(window, out var entry))
            {
                return;
            }

            if (entry.Count > 1)
            {
                s_suspendedWindows[window] = (entry.Service, entry.Count - 1);
                return;
            }

            s_suspendedWindows.Remove(window);
            s_toolTipServiceField!.SetValue(window, entry.Service);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not resume render-driven tooltip updates for {Window}", window.GetType().Name);
        }
    }

    private static void OnPointerEntered(Control control, PointerEventArgs e)
    {
        DisableRenderToolTipUpdates(control);

        // Entered is raised from the innermost control outwards, so a control that contains the current one is ignored
        // and the innermost control with a tip wins.
        if (IsEligible(control) && !IsAncestorOfCurrent(control))
        {
            Start(control);
        }
    }

    private static bool IsAncestorOfCurrent(Control control)
    {
        return s_current != null && s_current.GetVisualAncestors().Contains(control);
    }

    private static void OnPointerMoved(Control control, PointerEventArgs e)
    {
        if (s_current == null && control.IsPointerOver && IsEligible(control))
        {
            Start(control);
        }
    }

    private static void OnPointerExited(Control control, PointerEventArgs e)
    {
        if (control != s_current)
        {
            return;
        }

        // Opening the tooltip's own window can make the dialog report that the pointer left while it is still over the
        // control. Only a pointer that is really outside the control closes the tooltip.
        if (new Rect(control.Bounds.Size).Contains(e.GetPosition(control)))
        {
            return;
        }

        Stop();

        // Leaving an inner control that is still inside another with a tip shows the outer tip, as Avalonia would.
        var outer = control.GetVisualAncestors().OfType<Control>().FirstOrDefault(c => c.IsPointerOver && IsEligible(c));
        if (outer != null)
        {
            Start(outer);
        }
    }

    private static void Start(Control control)
    {
        Stop();

        s_current = control;
        var cancellation = new CancellationTokenSource();
        s_cancellation = cancellation;

        DispatcherTimer.RunOnce(
            () =>
            {
                if (cancellation.IsCancellationRequested || s_current != control)
                {
                    return;
                }

                // If Avalonia opened the tooltip itself there is nothing to do.
                if (control.IsPointerOver && !ToolTip.GetIsOpen(control))
                {
                    ToolTip.SetIsOpen(control, true);
                    s_openedByUs = control;
                }
            },
            TimeSpan.FromMilliseconds(Math.Max(ToolTip.GetShowDelay(control), 0)));
    }

    private static void Stop()
    {
        s_cancellation?.Cancel();
        s_cancellation = null;

        if (s_openedByUs != null)
        {
            ToolTip.SetIsOpen(s_openedByUs, false);
            s_openedByUs = null;
        }

        s_current = null;
    }
}
