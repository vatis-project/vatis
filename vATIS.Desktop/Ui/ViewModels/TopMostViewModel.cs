// <copyright file="TopMostViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model responsible for managing the "Always On Top" behavior for the <see cref="MainWindow"/>.
/// </summary>
public class TopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? appConfig;
    private bool isTopMost;

    private TopMostViewModel()
    {
        this.ToggleIsTopMost = ReactiveCommand.Create(this.HandleToggleIsTopMost);
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="TopMostViewModel"/> class.
    /// </summary>
    public static TopMostViewModel Instance { get; } = new();

    /// <summary>
    /// Gets the reactive command that toggles the "Always On Top" state of the application window.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the command execution encounters an unexpected error.
    /// </exception>
    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application window is set to "Always On Top".
    /// </summary>
    public bool IsTopMost
    {
        get => this.isTopMost;
        set => this.RaiseAndSetIfChanged(ref this.isTopMost, value);
    }

    /// <summary>
    /// Initializes the state of the <see cref="TopMostViewModel"/> instance using the given application configuration.
    /// </summary>
    /// <param name="config">The application configuration used to initialize the view model.</param>
    public void Initialize(IAppConfig config)
    {
        this.appConfig = config;
        this.IsTopMost = this.appConfig.AlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        this.IsTopMost = !this.IsTopMost;

        if (this.appConfig != null)
        {
            this.appConfig.AlwaysOnTop = this.IsTopMost;
            this.appConfig.SaveConfig();
        }
    }
}
