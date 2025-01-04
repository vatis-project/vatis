using DevServer.Hub;
using DevServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DevServer.Controllers;

[ApiController, Route("metar")]
public class MetarController : ControllerBase
{
    private readonly IMetarRepository _metarRepository;
    private readonly IHubContext<ClientHub, IClientHub> _hubContext;

    public MetarController(IMetarRepository metarRepository, IHubContext<ClientHub, IClientHub> hubContext)
    {
        _metarRepository = metarRepository;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetar([FromQuery] string id)
    {
        var metar = await _metarRepository.GetVatsimMetar(id);
        return Ok(metar);
    }

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