using BlueHarbor.Data;
using BlueHarbor.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlueHarbor.Services;

/// <summary>
/// Gestisce il tempo virtuale del terminal (nessun real-time).
/// </summary>
public class TerminalClockService
{
    public const long ClockId = 1L;

    private readonly AppDbContext _db;

    public TerminalClockService(AppDbContext db)
    {
        _db = db;
    }

    public int GetCurrentDay()
    {
        return Clock().CurrentDay;
    }

    /// <summary>
    /// Azione "Next Day": avanza il giorno di 1 e imposta DEPARTED per le navi
    /// la cui finestra di occupazione e' terminata. Nessuna assegnazione automatica.
    /// </summary>
    public int NextDay()
    {
        TerminalClock clock = Clock();
        int newDay = clock.CurrentDay + 1;
        clock.CurrentDay = newDay;

        var assigned = _db.Ships
            .Where(s => s.Status == ShipStatus.ASSIGNED)
            .OrderBy(s => s.ArrivalDay)
            .ToList();

        foreach (Ship ship in assigned)
        {
            Assignment? assignment = _db.Assignments.FirstOrDefault(a => a.ShipId == ship.Id);
            // endDay e' esclusivo: al giorno endDay la banchina e' libera e la nave e' partita.
            if (assignment is not null && assignment.EndDay <= newDay)
            {
                ship.Status = ShipStatus.DEPARTED;
            }
        }

        _db.SaveChanges();
        return newDay;
    }

    private TerminalClock Clock()
    {
        return _db.TerminalClocks.FirstOrDefault(c => c.Id == ClockId)
               ?? throw new InvalidOperationException("TerminalClock non inizializzato");
    }
}
