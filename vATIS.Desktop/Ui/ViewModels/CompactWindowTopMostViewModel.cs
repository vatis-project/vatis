// <copyright file="CompactWindowTopMostViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a view model for managing the "Always On Top" functionality of a compact window in the application.
/// </summary>
public class CompactWindowTopMostViewModel : ReactiveViewModelBase
{
    private static readonly Lazy<CompactWindowTopMostViewModel> InstanceValue = new(
        () => new CompactWindowTopMostViewModel());

    private IAppConfig? appConfig;
    private bool isTopMost;

    private CompactWindowTopMostViewModel()
    {
        this.ToggleIsTopMost = ReactiveCommand.Create(this.HandleToggleIsTopMost);
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="CompactWindowTopMostViewModel"/> class.
    /// </summary>
    public static CompactWindowTopMostViewModel Instance => InstanceValue.Value;

    /// <summary>
    /// Gets the command that toggles the "Always On Top" state of the compact window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the compact window is displayed as the topmost window.
    /// </summary>
    public bool IsTopMost
    {
        get => this.isTopMost;
        set => this.RaiseAndSetIfChanged(ref this.isTopMost, value);
    }

    /// <summary>
    /// Initializes the <see cref="CompactWindowTopMostViewModel"/> with the specified application configuration.
    /// </summary>
    /// <param name="config">The application configuration used to initialize the view model.</param>
    public void Initialize(IAppConfig config)
    {
        this.appConfig = config;
        this.IsTopMost = this.appConfig.CompactWindowAlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        this.IsTopMost = !this.IsTopMost;

        if (this.appConfig != null)
        {
            this.appConfig.CompactWindowAlwaysOnTop = this.IsTopMost;
            this.appConfig.SaveConfig();
        }
    }
}
