using BlueHarbor.Domain;
using BlueHarbor.Security;
using BlueHarbor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueHarbor.Controllers;

/// <summary>
/// Area Operatore: crea e mantiene le navi. Non gestisce l'assegnazione delle banchine.
/// </summary>
[Route("ships")]
[Authorize(Roles = Roles.Operatore)]
public class ShipController : Controller
{
    private readonly ShipService _shipService;

    public ShipController(ShipService shipService)
    {
        _shipService = shipService;
    }

    [HttpGet("")]
    public IActionResult List()
    {
        return View("List", _shipService.FindAll());
    }

    [HttpGet("new")]
    public IActionResult NewForm()
    {
        return View("Form");
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(string name, string? notes)
    {
        Ship ship = _shipService.Create(name, notes);
        TempData["message"] =
            $"Nave \"{ship.Name}\" registrata (dimensione {ship.Size}, arrivo giorno " +
            $"{ship.ArrivalDay}, durata {ship.OccupationDuration} giorni).";
        return Redirect("/ships");
    }

    [HttpGet("{id:long}/edit")]
    public IActionResult EditForm(long id)
    {
        return View("Edit", _shipService.Get(id));
    }

    [HttpPost("{id:long}")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(long id, string name, string? notes)
    {
        _shipService.UpdateMetadata(id, name, notes);
        TempData["message"] = "Nave aggiornata.";
        return Redirect("/ships");
    }
}
