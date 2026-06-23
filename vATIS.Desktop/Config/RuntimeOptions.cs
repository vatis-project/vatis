// <copyright file="RuntimeOptions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Config;

/// <summary>
/// Provides process-wide runtime options derived from startup arguments.
/// </summary>
public static class RuntimeOptions
{
    private const string DefaultVoiceServerUrl = "https://voice1.vatsim.net";
    private const string DownstairsGeekVoiceServerUrl = "https://afv.downstairsgeek.com";
    private const string DownstairsGeekFsdServerHost = "e.downstairsgeek.com";

    /// <summary>
    /// Gets a value indicating whether the client is running in Downstairs Geek mode.
    /// </summary>
    public static bool UseDownstairsGeekNetwork { get; private set; }

    /// <summary>
    /// Gets the configured voice server URL for the current process.
    /// </summary>
    public static string VoiceServerUrl =>
        UseDownstairsGeekNetwork ? DownstairsGeekVoiceServerUrl : DefaultVoiceServerUrl;

    /// <summary>
    /// Gets the configured FSD server host for the current process, if overridden.
    /// </summary>
    public static string? FsdServerHost => UseDownstairsGeekNetwork ? DownstairsGeekFsdServerHost : null;

    /// <summary>
    /// Gets a value indicating whether the ATIS hub should be enabled.
    /// </summary>
    public static bool IsAtisHubEnabled => !UseDownstairsGeekNetwork;

    /// <summary>
    /// Applies runtime options based on startup arguments.
    /// </summary>
    /// <param name="useDownstairsGeekNetwork">True to force the Downstairs Geek network endpoints.</param>
    public static void Configure(bool useDownstairsGeekNetwork)
    {
        UseDownstairsGeekNetwork = useDownstairsGeekNetwork;
    }
}
