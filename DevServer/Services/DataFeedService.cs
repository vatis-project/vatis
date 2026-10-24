// <copyright file="DataFeedService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Services;

/// <summary>
/// Holds the controllers shown as online in the local copy of the VATSIM data feed.
/// </summary>
public class DataFeedService
{
    private const int ObserverFacility = 0;

    private static readonly Dictionary<string, int> s_facilities = new()
    {
        ["OBS"] = ObserverFacility,
        ["FSS"] = 1,
        ["DEL"] = 2,
        ["GND"] = 3,
        ["TWR"] = 4,
        ["APP"] = 5,
        ["DEP"] = 5,
        ["CTR"] = 6,
    };

    private readonly object _lock = new();
    private List<FeedController> _controllers = [];

    /// <summary>
    /// Works out the facility type from the suffix of a callsign, e.g. <c>_TWR</c> is a tower. A callsign without a
    /// known suffix is an observer.
    /// </summary>
    /// <param name="callsign">The callsign.</param>
    /// <returns>The facility type, as in the VATSIM data feed.</returns>
    public static int GetFacility(string callsign)
    {
        var suffix = callsign.Trim().ToUpperInvariant();
        suffix = suffix[(suffix.LastIndexOf('_') + 1)..];
        return s_facilities.GetValueOrDefault(suffix, ObserverFacility);
    }

    /// <summary>
    /// Gets the controllers currently in the feed.
    /// </summary>
    /// <returns>A copy of the controller list.</returns>
    public List<FeedController> GetControllers()
    {
        lock (_lock)
        {
            return [.. _controllers];
        }
    }

    /// <summary>
    /// Replaces the controllers in the feed.
    /// </summary>
    /// <param name="callsigns">The new callsigns. Blank ones are dropped, and duplicates are kept once. The suffix of
    /// each callsign sets the facility type.</param>
    public void SetControllers(IEnumerable<string> callsigns)
    {
        var cleaned = callsigns
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .Select(x => new FeedController(x, GetFacility(x)))
            .ToList();

        lock (_lock)
        {
            _controllers = cleaned;
        }
    }
}
