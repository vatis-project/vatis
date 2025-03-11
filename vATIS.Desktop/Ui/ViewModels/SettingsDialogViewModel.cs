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
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the settings dialog, managing user settings and interaction logic.
/// </summary>
public class SettingsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;
    private string? _name;
    private string? _userId;
    private string? _password;
    private string? _selectedNetworkRating;
    private ObservableCollection<ComboBoxItemMeta>? _networkRatings;
    private bool _suppressNotificationSound;
    private bool _autoFetchAtisLetter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialogViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration interface providing settings for the view model.</param>
    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        _appConfig = appConfig;

        Name = _appConfig.Name;
        UserId = _appConfig.UserId;
        Password = _appConfig.PasswordDecrypted;
        SuppressNotificationSound = _appConfig.SuppressNotificationSound;
        SelectedNetworkRating = _appConfig.NetworkRating.ToString();
        AutoFetchAtisLetter = _appConfig.AutoFetchAtisLetter;

        NetworkRatings =
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
            new ComboBoxItemMeta("Administrator", "ADM"),
        ];

        SaveSettingsCommand = ReactiveCommand.Create<ICloseable>(SaveSettings);
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
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    /// <summary>
    /// Gets or sets the decrypted password associated with the settings dialog.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    /// <summary>
    /// Gets or sets the currently selected network rating in the settings dialog.
    /// </summary>
    public string? SelectedNetworkRating
    {
        get => _selectedNetworkRating;
        set => this.RaiseAndSetIfChanged(ref _selectedNetworkRating, value);
    }

    /// <summary>
    /// Gets or sets the collection of network ratings available for selection.
    /// </summary>
    public ObservableCollection<ComboBoxItemMeta>? NetworkRatings
    {
        get => _networkRatings;
        set => this.RaiseAndSetIfChanged(ref _networkRatings, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether notification sounds should be suppressed.
    /// </summary>
    public bool SuppressNotificationSound
    {
        get => _suppressNotificationSound;
        set => this.RaiseAndSetIfChanged(ref _suppressNotificationSound, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically fetch real-world ATIS letter.
    /// </summary>
    public bool AutoFetchAtisLetter
    {
        get => _autoFetchAtisLetter;
        set => this.RaiseAndSetIfChanged(ref _autoFetchAtisLetter, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SaveSettingsCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void SaveSettings(ICloseable window)
    {
        _appConfig.Name = Name ?? "";
        _appConfig.UserId = UserId?.Trim() ?? "";
        _appConfig.PasswordDecrypted = Password?.Trim() ?? "";
        _appConfig.SuppressNotificationSound = SuppressNotificationSound;
        _appConfig.AutoFetchAtisLetter = AutoFetchAtisLetter;

        if (Enum.TryParse(SelectedNetworkRating, out NetworkRating selectedNetworkRating))
        {
            _appConfig.NetworkRating = selectedNetworkRating;
        }

        _appConfig.SaveConfig();

        EventBus.Instance.Publish(new GeneralSettingsUpdated());

        window.Close();
    }
}
