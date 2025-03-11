// <copyright file="StartupWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Disposables;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the startup window.
/// </summary>
public class StartupWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private string _status = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindowViewModel"/> class.
    /// </summary>
    public StartupWindowViewModel()
    {
        _disposables.Add(EventBus.Instance.Subscribe<StartupStatusChanged>(evt =>
        {
            Status = evt.Status;
        }));
    }

    /// <summary>
    /// Gets or sets the current status of the startup process.
    /// </summary>
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        GC.SuppressFinalize(this);
    }
}
