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

        Opened += OnOpened;
        Loaded += OnLoaded;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    public ProfileListDialog()
    {
        InitializeComponent();
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
        ViewModel?.Dispose();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PositionChanged += OnPositionChanged;
        ViewModel?.InitializeCommand.Execute().Subscribe();
        ViewModel?.RestorePosition(this);
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
