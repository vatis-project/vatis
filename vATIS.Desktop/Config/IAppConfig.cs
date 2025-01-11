// <copyright file="IAppConfig.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Network;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the configuration interface for the application.
/// </summary>
public interface IAppConfig
{
    /// <summary>
    /// Gets or sets the user's real name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the user's VATSIM ID.
    /// </summary>
    string UserId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted password.
    /// </summary>
    string Password { get; set; }

    /// <summary>
    /// Gets or sets the decrypted password.
    /// </summary>
    string PasswordDecrypted { get; set; }

    /// <summary>
    /// Gets or sets the user's VATSIM network rating.
    /// </summary>
    NetworkRating NetworkRating { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether notification sounds should be suppressed.
    /// </summary>
    bool SuppressNotificationSound { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application window should always remain on top of other windows.
    /// </summary>
    bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the compact window should always be on top.
    /// </summary>
    bool CompactWindowAlwaysOnTop { get; set; }

    /// <summary>
    /// Gets a value indicating whether app configuration is required.
    /// </summary>
    bool ConfigRequired { get; }

    /// <summary>
    /// Gets or sets the microphone device used for audio recording.
    /// </summary>
    string? MicrophoneDevice { get; set; }

    /// <summary>
    /// Gets or sets the playback device used for audio output.
    /// </summary>
    string? PlaybackDevice { get; set; }

    /// <summary>
    /// Gets or sets the position of the main window.
    /// </summary>
    WindowPosition? MainWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the compact window.
    /// </summary>
    WindowPosition? CompactWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the Profile List Dialog window.
    /// </summary>
    WindowPosition? ProfileListDialogWindowPosition { get; set; }

    /// <summary>
    /// Gets or sets the position of the Voice Record ATIS Dialog window.
    /// </summary>
    WindowPosition? VoiceRecordAtisDialogWindowPosition { get; set; }

    /// <summary>
    /// Loads the application configuration from the config file.
    /// </summary>
    void LoadConfig();

    /// <summary>
    /// Saves the current application configuration.
    /// </summary>
    void SaveConfig();
}
