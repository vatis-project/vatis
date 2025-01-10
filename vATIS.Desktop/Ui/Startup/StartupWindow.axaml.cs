// <copyright file="StartupWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Startup;

/// <summary>
/// Represents the startup window of the application.
/// </summary>
public partial class StartupWindow : ReactiveWindow<StartupWindowViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the startup window.</param>
    public StartupWindow(StartupWindowViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    public StartupWindow()
    {
        this.InitializeComponent();
    }
}
