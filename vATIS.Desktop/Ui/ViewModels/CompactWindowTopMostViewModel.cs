// <copyright file="CompactWindowTopMostViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

public class CompactWindowTopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? _appConfig;

    private static readonly Lazy<CompactWindowTopMostViewModel> s_instance = new(() =>
        new CompactWindowTopMostViewModel());
    public static CompactWindowTopMostViewModel Instance => s_instance.Value;

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    private bool _isTopMost;
    public bool IsTopMost
    {
        get => _isTopMost;
        set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
    }

    private CompactWindowTopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    public void Initialize(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        IsTopMost = _appConfig.CompactWindowAlwaysOnTop;
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
