// <copyright file="MetarController.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Hub;
using DevServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DevServer.Controllers;

/// <summary>
/// Represents a controller for METAR data.
/// </summary>
[ApiController]
[Route("metar")]
public class MetarController : ControllerBase
{
    private readonly IMetarRepository _metarRepository;
    private readonly IHubContext<ClientHub, IClientHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarController"/> class.
    /// </summary>
    /// <param name="metarRepository">The METAR repository.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    public MetarController(IMetarRepository metarRepository, IHubContext<ClientHub, IClientHub> hubContext)
    {
        _metarRepository = metarRepository;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Gets the METAR data for the specified ICAO airport identifier.
    /// </summary>
    /// <param name="id">The ICAO airport identifier.</param>
    /// <returns>The METAR data for the specified airport identifier.</returns>
    [HttpGet]
    public async Task<IActionResult> GetMetar([FromQuery] string id)
    {
        var metar = await _metarRepository.GetVatsimMetar(id);
        return Ok(metar);
    }

    /// <summary>
    /// Updates the METAR data for a specified airport.
    /// </summary>
    /// <param name="metar">The METAR data to update.</param>
    /// <returns>A response indicating the success of the operation.</returns>
    [HttpPost]
    public async Task<IActionResult> UpdateMetar([FromBody] string metar)
    {
        if (!string.IsNullOrWhiteSpace(metar))
        {
            await _hubContext.Clients.All.MetarReceived(metar);
            return Ok();
        }

        return BadRequest();
    }
}
