using BlueHarbor.Data;
using BlueHarbor.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlueHarbor.Services;

/// <summary>
/// Casi d'uso dello Scheduler: elenco navi Pending, board delle banchine e assegnazione.
/// </summary>
public class SchedulingService
{
    private readonly AppDbContext _db;
    private readonly TerminalClockService _clockService;

    public SchedulingService(AppDbContext db, TerminalClockService clockService)
    {
        _db = db;
        _clockService = clockService;
    }

    public List<Ship> PendingShips()
    {
        return _db.Ships
            .Where(s => s.Status == ShipStatus.PENDING)
            .OrderBy(s => s.ArrivalDay)
            .ToList();
    }

    /// <summary>
    /// Stato di tutte le banchine per la board dello Scheduler.
    /// </summary>
    public List<BerthOccupancy> Board()
    {
        int currentDay = _clockService.GetCurrentDay();
        var rows = new List<BerthOccupancy>();
        foreach (Berth berth in _db.Berths.OrderBy(b => b.Code).ToList())
        {
            List<Assignment> assignments = AssignmentsOf(berth);
            var windows = assignments
                .Select(a => new OccupiedWindow(a.Ship.Name, a.StartDay, a.EndDay))
                .ToList();
            bool occupiedNow = assignments.Any(a => a.StartDay <= currentDay && currentDay < a.EndDay);
            int nextFreeDay = FirstAvailableStart(berth, currentDay, 1);
            rows.Add(new BerthOccupancy(berth.Id, berth.Code, berth.Size,
                occupiedNow, nextFreeDay, windows));
        }
        return rows;
    }

    /// <summary>
    /// Assegna una nave Pending a una banchina compatibile, pianificandola nel
    /// primo slot temporale disponibile (>= giorno di arrivo). Stato -> Assigned.
    /// </summary>
    public Assignment Assign(long shipId, long berthId)
    {
        Ship ship = _db.Ships.FirstOrDefault(s => s.Id == shipId)
                    ?? throw DomainException.NotFound($"Nave non trovata: {shipId}");
        Berth berth = _db.Berths.FirstOrDefault(b => b.Id == berthId)
                      ?? throw DomainException.NotFound($"Banchina non trovata: {berthId}");

        if (ship.Status != ShipStatus.PENDING)
        {
            throw DomainException.Conflict("La nave non e' in stato Pending");
        }
        if (berth.Size != ship.Size)
        {
            throw DomainException.Conflict(
                $"Banchina {berth.Code} ({berth.Size}) incompatibile con nave {ship.Size}");
        }

        int startDay = FirstAvailableStart(berth, ship.ArrivalDay, ship.OccupationDuration);
        int endDay = startDay + ship.OccupationDuration;

        var assignment = new Assignment(ship, berth, startDay, endDay);
        ship.Status = ShipStatus.ASSIGNED;
        _db.Assignments.Add(assignment);
        _db.SaveChanges();
        return assignment;
    }

    /// <summary>
    /// Primo giorno >= arrivalDay in cui la banchina e' libera per l'intera finestra
    /// [start, start+duration). Rispetta le occupazioni esistenti e gli eventuali buchi.
    /// </summary>
    internal int FirstAvailableStart(Berth berth, int arrivalDay, int duration)
    {
        List<Assignment> existing = AssignmentsOf(berth);
        int candidate = arrivalDay;
        bool moved = true;
        while (moved)
        {
            moved = false;
            foreach (Assignment a in existing)
            {
                bool overlap = a.StartDay < candidate + duration && a.EndDay > candidate;
                if (overlap)
                {
                    candidate = a.EndDay; // sposta la nave dopo l'occupazione in conflitto
                    moved = true;
                }
            }
        }
        return candidate;
    }

    // Assegnazioni di una banchina in ordine temporale: base per calcolare il primo slot libero.
    private List<Assignment> AssignmentsOf(Berth berth)
    {
        return _db.Assignments
            .Include(a => a.Ship)
            .Where(a => a.BerthId == berth.Id)
            .OrderBy(a => a.StartDay)
            .ToList();
    }
}
