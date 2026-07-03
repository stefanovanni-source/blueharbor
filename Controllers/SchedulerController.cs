using BlueHarbor.Domain;
using BlueHarbor.Security;
using BlueHarbor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueHarbor.Controllers;

/// <summary>
/// Area Scheduler: navi in attesa, board delle banchine e assegnazione.
/// </summary>
[Route("scheduler")]
[Authorize(Roles = Roles.Scheduler)]
public class SchedulerController : Controller
{
    private readonly SchedulingService _schedulingService;

    public SchedulerController(SchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    [HttpGet("")]
    public IActionResult Board()
    {
        ViewData["pendingShips"] = _schedulingService.PendingShips();
        ViewData["berths"] = _schedulingService.Board();
        return View("Board");
    }

    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public IActionResult Assign(long shipId, long berthId)
    {
        try
        {
            Assignment assignment = _schedulingService.Assign(shipId, berthId);
            TempData["message"] =
                $"Nave \"{assignment.Ship.Name}\" assegnata alla banchina {assignment.Berth.Code} " +
                $"dal giorno {assignment.StartDay} al {assignment.EndDay - 1}.";
        }
        catch (DomainException ex)
        {
            TempData["error"] = ex.Message;
        }
        return Redirect("/scheduler");
    }
}
