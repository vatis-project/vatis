// <copyright file="ProfileListDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Serilog;
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
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Loaded += this.ProfileListDialog_Loaded;
        this.Closed += this.OnClosed;
        this.Closing += this.OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    public ProfileListDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the dialog is opened.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (this.DataContext is ProfileListViewModel model)
        {
            model.SetDialogOwner(this);
        }
    }

    /// <summary>
    /// Handles the logic that occurs when the <see cref="ProfileListDialog"/> is loaded.
    /// </summary>
    /// <param name="e">The event data associated with the loaded event.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is ProfileListViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
        if (!e.IsProgrammatic)
        {
            // Execute the ExitCommand to perform a clean application shutdown
            Dispatcher.UIThread.InvokeAsync(() => this.ViewModel?.ExitCommand.Execute().Subscribe());
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private async void ProfileListDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await this.ViewModel?.InitializeCommand.Execute()!;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load profile list");
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is ProfileListViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
