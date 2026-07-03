using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueHarbor.Controllers;

/// <summary>Endpoint di health/status.</summary>
[ApiController]
[Route("api")]
[AllowAnonymous]
public class StatusController : ControllerBase
{
    /// <summary>Restituisce lo stato del servizio: HTTP 200 se attivo.</summary>
    [HttpGet("status")]
    public ActionResult<string> Status()
    {
        return Ok("OK");
    }
}
