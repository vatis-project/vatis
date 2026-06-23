// <copyright file="IAppConfig.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Network;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the configuration interface required for the application.
/// </summary>
public interface IAppConfig
{
    /// <summary>
    /// Gets or sets the user's real name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the user's VATSIM ID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's encrypted password.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the user's decrypted password.
    /// </summary>
    public string PasswordDecrypted { get; set; }

    /// <summary>
    /// Gets or sets the user's network rating.
    /// </summary>
    public NetworkRating NetworkRating { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress update sound for own ATIS updates.
    /// </summary>
    public bool MuteOwnAtisUpdateSound { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress update sound for shared ATIS updates.
    /// </summary>
    public bool MuteSharedAtisUpdateSound { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application's main window should always remain on top of other windows.
    /// </summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mini-window should always remain on top of other windows.
    /// </summary>
    public bool MiniWindowAlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mini-window should show the full METAR details.
    /// </summary>
    public bool MiniWindowShowMetarDetails { get; set; }

    /// <summary>
    /// Gets a value indicating whether the configuration is required.
    /// </summary>
    public bool ConfigRequired { get; }

    /// <summary>
    /// Gets or sets the name of the selected microphone device.
    /// </summary>
    public string? MicrophoneDevice { get; set; }

    /// <summary>
    /// Gets or sets the name of the selected playback device.
    /// </summary>
    public string? PlaybackDevice { get; set; }

    /// <summary>
    /// Gets or sets the position of the main application window.
    /// </summary>
    public WindowPosition? MainWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the mini-window.
    /// </summary>
    public WindowPosition? MiniWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the Profile List Dialog window on the screen.
    /// </summary>
    public WindowPosition? ProfileListDialogWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the Voice Record ATIS dialog window.
    /// </summary>
    public WindowPosition? VoiceRecordAtisDialogWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-fetch the ATIS letter.
    /// </summary>
    public bool AutoFetchAtisLetter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress the release notes window after updating.
    /// </summary>
    public bool SuppressReleaseNotes { get; set; }

    /// <summary>
    /// Loads the configuration settings for the application.
    /// </summary>
    public void LoadConfig();

    /// <summary>
    /// Saves the current application configuration settings to persistent storage.
    /// </summary>
    public void SaveConfig();
}
