// <copyright file="FeedController.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Services;

/// <summary>
/// A controller in the local data feed.
/// </summary>
/// <param name="Callsign">The callsign.</param>
/// <param name="Facility">The facility type, as in the VATSIM data feed: 0 observer, 1 FSS, 2 delivery, 3 ground,
/// 4 tower, 5 approach or departure, 6 center.</param>
public record FeedController(string Callsign, int Facility);
