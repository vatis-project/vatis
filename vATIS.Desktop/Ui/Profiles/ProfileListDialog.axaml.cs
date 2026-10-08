// <copyright file="ProfileListDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Vatsim.Vatis.Ui.Extensions;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Profiles;

/// <summary>
/// Represents a dialog window for displaying and managing a list of profiles.
/// </summary>
public partial class ProfileListDialog : ReactiveWindow<ProfileListViewModel>, IDialogOwner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The ViewModel for this dialog.</param>
    public ProfileListDialog(ProfileListViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        RestorePositionBeforeShow();

        Opened += OnOpened;
        Loaded += OnLoaded;
        Closed += OnClosed;
        Closing += OnClosing;
        KeyDown += OnWindowKeyDown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    public ProfileListDialog()
    {
        InitializeComponent();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        AppDataShortcut.HandleKeyDown(this, e);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        ViewModel?.SetDialogOwner(this);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
        if (!e.IsProgrammatic)
        {
            // Execute the ExitCommand to perform a clean application shutdown
            Dispatcher.UIThread.InvokeAsync(() => ViewModel?.ExitCommand.Execute().Subscribe());
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        KeyDown -= OnWindowKeyDown;
        ViewModel?.Dispose();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PositionChanged += OnPositionChanged;
        ViewModel?.InitializeCommand.Execute().Subscribe();
        if (WindowStartupLocation != WindowStartupLocation.Manual)
        {
            ViewModel?.RestorePosition(this);
        }
    }

    private void RestorePositionBeforeShow()
    {
        // Restore the saved position before the window is shown. Restoring after it has been shown makes the window
        // appear at the default (centered) location first and then visibly jump to the saved one.
        var defaultPosition = Position;
        ViewModel?.RestorePosition(this);
        if (Position != defaultPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (DataContext is ProfileListViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
