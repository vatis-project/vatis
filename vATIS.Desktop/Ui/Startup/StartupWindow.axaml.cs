// <copyright file="StartupWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Startup;

public partial class StartupWindow : ReactiveWindow<StartupWindowViewModel>
{
    public StartupWindow(StartupWindowViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public StartupWindow()
    {
        InitializeComponent();
    }
}