using BlueHarbor.Domain;
using BlueHarbor.Services;

namespace BlueHarbor.Data;

/// <summary>
/// Popola all'avvio i dati fissi del dominio:
///  - l'orologio virtuale (giorno 1)
///  - l'insieme fisso di banchine: 1 XL, 1 L, 2 M, 4 S
/// </summary>
public static class DataInitializer
{
    public static void Seed(AppDbContext db)
    {
        if (!db.TerminalClocks.Any())
        {
            db.TerminalClocks.Add(new TerminalClock(TerminalClockService.ClockId, 1));
        }

        if (!db.Berths.Any())
        {
            db.Berths.AddRange(
                new Berth("XL1", Size.XL),
                new Berth("L1", Size.L),
                new Berth("M1", Size.M),
                new Berth("M2", Size.M),
                new Berth("S1", Size.S),
                new Berth("S2", Size.S),
                new Berth("S3", Size.S),
                new Berth("S4", Size.S));
        }

        db.SaveChanges();
    }
}
