// <copyright file="PathProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;

namespace Vatsim.Vatis.Io;

/// <summary>
/// Provides paths for various application data files.
/// </summary>
public static class PathProvider
{
    private static string appDataPath = string.Empty;

    /// <summary>
    /// Gets the folder path for logs.
    /// </summary>
    public static string LogsFolderPath => Path.Combine(appDataPath, "Logs");

    /// <summary>
    /// Gets the folder path for profiles.
    /// </summary>
    public static string ProfilesFolderPath => Path.Combine(appDataPath, "Profiles");

    /// <summary>
    /// Gets the file path for the application configuration.
    /// </summary>
    public static string AppConfigFilePath => Path.Combine(appDataPath, "AppConfig.json");

    /// <summary>
    /// Gets the file path for the airport data.
    /// </summary>
    public static string AirportsFilePath => Path.Combine(appDataPath, "Airports.json");

    /// <summary>
    /// Gets the file path for the Navaids data.
    /// </summary>
    public static string NavaidsFilePath => Path.Combine(appDataPath, "Navaids.json");

    /// <summary>
    /// Gets the file path for the NavData serial data.
    /// </summary>
    public static string NavDataSerialFilePath => Path.Combine(appDataPath, "NavDataSerial.json");

    /// <summary>
    /// Gets the file path for a profile based on the profile ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The file path for the profile.</returns>
    public static string GetProfilePath(string profileId)
    {
        return Path.Combine(ProfilesFolderPath, profileId + ".json");
    }

    /// <summary>
    /// Sets the application data path.
    /// </summary>
    /// <param name="path">The application data path.</param>
    public static void SetAppDataPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException(@"Path cannot be null or empty", nameof(path));
        }

        appDataPath = path;
    }
}
