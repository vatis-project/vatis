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
    private readonly IProfileRepository profileRepository;
    private readonly IWindowFactory windowFactory;
    private MainWindow? mainWindow;
    private ProfileListDialog? profileListDialog;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="windowFactory">The window factory to create UI windows.</param>
    /// <param name="profileRepository">The profile repository to manage profiles.</param>
    public SessionManager(IWindowFactory windowFactory, IProfileRepository profileRepository)
    {
        this.windowFactory = windowFactory;
        this.profileRepository = profileRepository;
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
        this.ShowProfileListDialog();
    }

    /// <inheritdoc/>
    public async Task StartSession(string profileId)
    {
        var profile = (await this.profileRepository.LoadAll()).Find(p => p.Id == profileId);
        if (profile == null)
        {
            return;
        }

        this.profileListDialog?.Close();
        this.CurrentProfile = profile;
        this.CurrentConnectionCount = 0;
        this.mainWindow = this.windowFactory.CreateMainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = this.mainWindow;
        }

        this.mainWindow.Show();
    }

    /// <inheritdoc/>
    public void EndSession()
    {
        MessageBus.Current.SendMessage(new SessionEnded());
        this.CurrentProfile = null;
        this.CurrentConnectionCount = 0;
        this.mainWindow?.Close();
        this.ShowProfileListDialog();
    }

    private void ShowProfileListDialog()
    {
        this.profileListDialog = this.windowFactory.CreateProfileListDialog();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = this.profileListDialog;
        }

        this.profileListDialog.Show();
    }
}
