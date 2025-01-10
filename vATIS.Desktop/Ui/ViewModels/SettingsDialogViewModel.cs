// <copyright file="SettingsDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the settings dialog, managing user settings and interaction logic.
/// </summary>
public class SettingsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig appConfig;
    private string? name;
    private ObservableCollection<ComboBoxItemMeta>? networkRatings;
    private string? password;
    private string? selectedNetworkRating;
    private bool suppressNotificationSound;
    private string? userId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialogViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration interface providing settings for the view model.</param>
    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        this.appConfig = appConfig;

        this.Name = this.appConfig.Name;
        this.UserId = this.appConfig.UserId;
        this.Password = this.appConfig.PasswordDecrypted;
        this.SuppressNotificationSound = this.appConfig.SuppressNotificationSound;
        this.SelectedNetworkRating = this.appConfig.NetworkRating.ToString();

        this.NetworkRatings =
        [
            new ComboBoxItemMeta("Observer", "OBS"),
            new ComboBoxItemMeta("Student 1", "S1"),
            new ComboBoxItemMeta("Student 2", "S2"),
            new ComboBoxItemMeta("Student 3", "S3"),
            new ComboBoxItemMeta("Controller 1", "C1"),
            new ComboBoxItemMeta("Controller 2", "C2"),
            new ComboBoxItemMeta("Controller 3", "C3"),
            new ComboBoxItemMeta("Instructor 1", "I1"),
            new ComboBoxItemMeta("Instructor 2", "I2"),
            new ComboBoxItemMeta("Instructor 3", "I3"),
            new ComboBoxItemMeta("Supervisor", "SUP"),
            new ComboBoxItemMeta("Administrator", "ADM")
        ];

        this.SaveSettingsCommand = ReactiveCommand.Create<ICloseable>(this.SaveSettings);
    }

    /// <summary>
    /// Gets the command used to save the user settings and close the associated dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Gets or sets the name associated with the current user configuration.
    /// </summary>
    public string? Name
    {
        get => this.name;
        set => this.RaiseAndSetIfChanged(ref this.name, value);
    }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId
    {
        get => this.userId;
        set => this.RaiseAndSetIfChanged(ref this.userId, value);
    }

    /// <summary>
    /// Gets or sets the decrypted password associated with the settings dialog.
    /// </summary>
    public string? Password
    {
        get => this.password;
        set => this.RaiseAndSetIfChanged(ref this.password, value);
    }

    /// <summary>
    /// Gets or sets the currently selected network rating in the settings dialog.
    /// </summary>
    public string? SelectedNetworkRating
    {
        get => this.selectedNetworkRating;
        set => this.RaiseAndSetIfChanged(ref this.selectedNetworkRating, value);
    }

    /// <summary>
    /// Gets or sets the collection of network ratings available for selection.
    /// </summary>
    public ObservableCollection<ComboBoxItemMeta>? NetworkRatings
    {
        get => this.networkRatings;
        set => this.RaiseAndSetIfChanged(ref this.networkRatings, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether notification sounds should be suppressed.
    /// </summary>
    public bool SuppressNotificationSound
    {
        get => this.suppressNotificationSound;
        set => this.RaiseAndSetIfChanged(ref this.suppressNotificationSound, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.SaveSettingsCommand.Dispose();
    }

    private void SaveSettings(ICloseable window)
    {
        this.appConfig.Name = this.Name ?? string.Empty;
        this.appConfig.UserId = this.UserId?.Trim() ?? string.Empty;
        this.appConfig.PasswordDecrypted = this.Password?.Trim() ?? string.Empty;
        this.appConfig.SuppressNotificationSound = this.SuppressNotificationSound;

        if (Enum.TryParse(this.SelectedNetworkRating, out NetworkRating selectedRating))
        {
            this.appConfig.NetworkRating = selectedRating;
        }

        this.appConfig.SaveConfig();

        MessageBus.Current.SendMessage(new GeneralSettingsUpdated());

        window.Close();
    }
}
