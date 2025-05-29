// <copyright file="MiniWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Serilog;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents the mini-window for displaying ATIS information in a compact view.
/// </summary>
public partial class MiniWindow : ReactiveWindow<MiniWindowViewModel>, ICloseable, IDialogOwner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MiniWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model to be used by the window.</param>
    public MiniWindow(MiniWindowViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.SetDialogOwner(this);

        Closed += OnClosed;
        Closing += OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniWindow"/> class.
    /// </summary>
    public MiniWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        ViewModel?.RestorePosition(this);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
            if (ViewModel is null || e.IsProgrammatic)
                return;

            e.Cancel = true;

            var shouldClose = await ViewModel.EndClientSessionCommand.Execute().FirstAsync();
            if (shouldClose)
            {
                e.Cancel = false;
                Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnClosing Exception");
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Button && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        ViewModel?.UpdatePosition(this);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Border { Name: "Root" })
        {
            ViewModel?.InvokeMainWindowCommand.Execute(this).Subscribe();
        }
    }
}
