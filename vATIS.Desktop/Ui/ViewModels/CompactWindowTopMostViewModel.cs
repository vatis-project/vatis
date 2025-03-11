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
public class CompactWindowTopMostViewModel : ReactiveViewModelBase, IDisposable
{
    private static readonly Lazy<CompactWindowTopMostViewModel> s_instance = new(() =>
        new CompactWindowTopMostViewModel());

    private IAppConfig? _appConfig;
    private bool _isTopMost;

    private CompactWindowTopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="CompactWindowTopMostViewModel"/> class.
    /// </summary>
    public static CompactWindowTopMostViewModel Instance => s_instance.Value;

    /// <summary>
    /// Gets the command that toggles the "Always On Top" state of the compact window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the compact window is displayed as the topmost window.
    /// </summary>
    public bool IsTopMost
    {
        get => _isTopMost;
        set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
    }

    /// <summary>
    /// Initializes the <see cref="CompactWindowTopMostViewModel"/> with the specified application configuration.
    /// </summary>
    /// <param name="appConfig">The application configuration used to initialize the view model.</param>
    public void Initialize(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        IsTopMost = _appConfig.CompactWindowAlwaysOnTop;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ToggleIsTopMost?.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleToggleIsTopMost()
    {
        IsTopMost = !IsTopMost;

        if (_appConfig != null)
        {
            _appConfig.CompactWindowAlwaysOnTop = IsTopMost;
            _appConfig.SaveConfig();
        }
    }
}
