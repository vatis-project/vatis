// <copyright file="IAppConfigurationProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Provides the configuration URLs for the application.
/// </summary>
public interface IAppConfigurationProvider
{
    /// <summary>
    /// Gets the URL of the version information for the application.
    /// </summary>
    string VersionUrl { get; }

    /// <summary>
    /// Gets the URL for retrieving METAR data.
    /// </summary>
    string MetarUrl { get; }

    /// <summary>
    /// Gets the URL of the navigation data configuration for the application.
    /// </summary>
    string NavDataUrl { get; }

    /// <summary>
    /// Gets the URL of the ATIS Hub for the application.
    /// </summary>
    string AtisHubUrl { get; }

    /// <summary>
    /// Gets the URL of the voice list for the application.
    /// </summary>
    string VoiceListUrl { get; }

    /// <summary>
    /// Gets the URL used for the text-to-speech service in the application.
    /// </summary>
    string TextToSpeechUrl { get; }

    /// <summary>
    /// Provides configuration settings and URLs for the application.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Initialize();
}
