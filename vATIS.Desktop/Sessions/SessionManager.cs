// <copyright file="SessionManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Sessions;

/// <inheritdoc />
public class SessionManager : ISessionManager
{
    private readonly IWindowFactory _windowFactory;
    private readonly IProfileRepository _profileRepository;
    private MainWindow? _mainWindow;
    private ProfileListDialog? _profileListDialog;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="windowFactory">The window factory to create UI windows.</param>
    /// <param name="profileRepository">The profile repository to manage profiles.</param>
    public SessionManager(IWindowFactory windowFactory, IProfileRepository profileRepository)
    {
        _windowFactory = windowFactory;
        _profileRepository = profileRepository;
    }

    /// <inheritdoc/>
    public int MaxConnectionCount => 4;

    /// <inheritdoc/>
    public int CurrentConnectionCount { get; set; }

    /// <inheritdoc/>
    public Profile? CurrentProfile { get; private set; }

    /// <inheritdoc/>
    public void Run()
    {
        ShowProfileListDialog();
    }

    /// <inheritdoc />
    public async Task StartSession(string profileId)
    {
        var profile = (await _profileRepository.LoadAll()).Find(p => p.Id == profileId);
        if (profile == null)
            return;

        _profileListDialog?.Close();
        CurrentProfile = profile;
        CurrentConnectionCount = 0;
        _mainWindow = _windowFactory.CreateMainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _mainWindow;
        }

        _mainWindow.Show();
    }

    /// <inheritdoc />
    public void EndSession()
    {
        MessageBus.Current.SendMessage(new SessionEnded());
        CurrentProfile = null;
        CurrentConnectionCount = 0;
        _mainWindow?.Close();
        ShowProfileListDialog();
    }

    private void ShowProfileListDialog()
    {
        _profileListDialog = _windowFactory.CreateProfileListDialog();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _profileListDialog;
        }

        _profileListDialog.Show();
    }
}
