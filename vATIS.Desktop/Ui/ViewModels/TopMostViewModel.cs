// <copyright file="TopMostViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;
using System.Reactive;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;
public class TopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? _appConfig;

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    private bool _isTopMost;
    public bool IsTopMost
    {
        get => _isTopMost;
        set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
    }

    public static TopMostViewModel Instance { get; } = new();

    private TopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    public void Initialize(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        IsTopMost = _appConfig.AlwaysOnTop;
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
