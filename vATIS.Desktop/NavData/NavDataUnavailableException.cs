// <copyright file="NavDataUnavailableException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Thrown when required navigation data is missing and could not be downloaded.
/// </summary>
public class NavDataUnavailableException() : Exception(
    "vATIS could not download the required navigation data. " +
    "Please check your internet connection and make sure your firewall or antivirus is not blocking vATIS, " +
    "then restart the application.");
