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

public partial class ProfileListDialog : ReactiveWindow<ProfileListViewModel>, IDialogOwner
{
    public ProfileListDialog(ProfileListViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += ProfileListDialog_Loaded;
        Closed += OnClosed;
        Closing += OnClosing;
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

    private async void ProfileListDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel?.InitializeCommand.Execute()!;
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
            BeginMoveDrag(e);
        }
    }

    public ProfileListDialog()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is ProfileListViewModel model)
        {
            model.SetDialogOwner(this);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        if (DataContext is ProfileListViewModel model)
        {
            model.RestorePosition(this);
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