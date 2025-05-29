// <copyright file="MiniWindowTopMostViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents a view model for managing the "Always On Top" functionality of the mini-window.
/// </summary>
public class MiniWindowTopMostViewModel : ReactiveViewModelBase, IDisposable
{
    private static readonly Lazy<MiniWindowTopMostViewModel> s_instance = new(() =>
        new MiniWindowTopMostViewModel());

    private IAppConfig? _appConfig;
    private bool _isTopMost;
    private bool _showMetarDetails;

    private MiniWindowTopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
        ToggleMetarDetails = ReactiveCommand.Create(HandleToggleMetarDetails);
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="MiniWindowTopMostViewModel"/> class.
    /// </summary>
    public static MiniWindowTopMostViewModel Instance => s_instance.Value;

    /// <summary>
    /// Gets the command that toggles the "Always On Top" state of the mini-window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    /// <summary>
    /// Gets the command that toggles the display of the METAR details (wind and altimeter) in the mini-window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleMetarDetails { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mini-window is displayed as the topmost window.
    /// </summary>
    public bool IsTopMost
    {
        get => _isTopMost;
        set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the METAR details (wind and altimeter) are displayed in
    /// the mini-window. If false, only the station ID and ATIS letter are shown.
    /// </summary>
    public bool ShowMetarDetails
    {
        get => _showMetarDetails;
        set => this.RaiseAndSetIfChanged(ref _showMetarDetails, value);
    }

    /// <summary>
    /// Initializes the <see cref="MiniWindowTopMostViewModel"/> with the specified application configuration.
    /// </summary>
    /// <param name="appConfig">The application configuration used to initialize the view model.</param>
    public void Initialize(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        IsTopMost = _appConfig.MiniWindowAlwaysOnTop;
        ShowMetarDetails = _appConfig.MiniWindowShowMetarDetails;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ToggleIsTopMost.Dispose();
        ToggleMetarDetails.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleToggleIsTopMost()
    {
        IsTopMost = !IsTopMost;

        if (_appConfig != null)
        {
            _appConfig.MiniWindowAlwaysOnTop = IsTopMost;
            _appConfig.SaveConfig();
        }
    }

    private void HandleToggleMetarDetails()
    {
        ShowMetarDetails = !ShowMetarDetails;

        if (_appConfig != null)
        {
            _appConfig.MiniWindowShowMetarDetails = ShowMetarDetails;
            _appConfig.SaveConfig();
        }
    }
}
