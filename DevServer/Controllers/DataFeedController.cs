// <copyright file="DataFeedController.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevServer.Controllers;

/// <summary>
/// Serves a local copy of the VATSIM data feed, and lets the web UI change which controllers are online.
/// </summary>
[ApiController]
[Route("datafeed")]
public class DataFeedController : ControllerBase
{
    private readonly DataFeedService _dataFeedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFeedController"/> class.
    /// </summary>
    /// <param name="dataFeedService">The data feed service.</param>
    public DataFeedController(DataFeedService dataFeedService)
    {
        _dataFeedService = dataFeedService;
    }

    /// <summary>
    /// Gets the data feed, in the shape of the VATSIM v3 data feed that vATIS reads.
    /// </summary>
    /// <returns>The data feed.</returns>
    [HttpGet]
    public IActionResult GetFeed()
    {
        return Ok(new
        {
            controllers = _dataFeedService.GetControllers(),
            atis = Array.Empty<object>(),
            pilots = Array.Empty<object>(),
        });
    }

    /// <summary>
    /// Gets the controllers in the feed.
    /// </summary>
    /// <returns>The controllers.</returns>
    [HttpGet("controllers")]
    public IActionResult GetControllers()
    {
        return Ok(_dataFeedService.GetControllers());
    }

    /// <summary>
    /// Replaces the controllers in the feed. The facility type of each is taken from the suffix of its callsign.
    /// </summary>
    /// <param name="callsigns">The callsigns of the new controllers.</param>
    /// <returns>The controllers now in the feed.</returns>
    [HttpPut("controllers")]
    public IActionResult SetControllers([FromBody] List<string> callsigns)
    {
        _dataFeedService.SetControllers(callsigns);
        return Ok(_dataFeedService.GetControllers());
    }
}
