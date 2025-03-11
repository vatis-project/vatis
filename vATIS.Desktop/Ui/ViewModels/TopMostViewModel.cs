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
public class TopMostViewModel : ReactiveViewModelBase, IDisposable
{
    private IAppConfig? _appConfig;
    private bool _isTopMost;

    private TopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="TopMostViewModel"/> class.
    /// </summary>
    public static TopMostViewModel Instance { get; } = new();

    /// <summary>
    /// Gets the reactive command that toggles the "Always On Top" state of the application window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application window is set to "Always On Top".
    /// </summary>
    public bool IsTopMost
    {
        get => _isTopMost;
        set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
    }

    /// <summary>
    /// Initializes the state of the <see cref="TopMostViewModel"/> instance using the given application configuration.
    /// </summary>
    /// <param name="appConfig">The application configuration used to initialize the view model.</param>
    public void Initialize(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        IsTopMost = _appConfig.AlwaysOnTop;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ToggleIsTopMost.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleToggleIsTopMost()
    {
        IsTopMost = !IsTopMost;

        if (_appConfig != null)
        {
            _appConfig.AlwaysOnTop = IsTopMost;
            _appConfig.SaveConfig();
        }
    }
}
