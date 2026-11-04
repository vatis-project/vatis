// <copyright file="AppRuntimeOptions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis;

/// <summary>
/// Holds process-wide runtime options derived from startup arguments.
/// </summary>
internal static class AppRuntimeOptions
{
    /// <summary>
    /// Gets the forced FSD host used when the DSG startup override is enabled.
    /// </summary>
    public const string DownstairsGeekFsdHost = "e.downstairsgeek.com";

    /// <summary>
    /// Gets the forced voice server URL used when the DSG startup override is enabled.
    /// </summary>
    public const string DownstairsGeekVoiceUrl = "https://afv.downstairsgeek.com";

    /// <summary>
    /// Gets or sets a value indicating whether DSG server overrides should be used.
    /// </summary>
    public static bool UseDownstairsGeekServers { get; set; }
}
