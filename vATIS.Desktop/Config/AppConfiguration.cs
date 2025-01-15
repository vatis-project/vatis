// <copyright file="AppConfiguration.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the configuration settings for the application, including various service URLs.
/// </summary>
public record AppConfiguration(
    string AtisHubUrl,
    string DigitalAtisApiUrl,
    string NavDataUrl,
    string TextToSpeechUrl,
    string VatsimStatusUrl,
    string VersionUrl,
    string VoiceListUrl);
