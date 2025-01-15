// <copyright file="StartupWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using ReactiveUI;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the startup window.
/// </summary>
public class StartupWindowViewModel : ReactiveViewModelBase
{
    private string _status = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindowViewModel"/> class.
    /// </summary>
    public StartupWindowViewModel()
    {
        MessageBus.Current.Listen<StartupStatusChanged>().Subscribe(evt =>
        {
            Status = evt.Status;
        });
    }

    /// <summary>
    /// Gets or sets the current status of the startup process.
    /// </summary>
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }
}
