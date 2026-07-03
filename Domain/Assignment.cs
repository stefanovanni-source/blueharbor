namespace BlueHarbor.Domain;

/// <summary>
/// Assegnazione di una nave a una banchina per una finestra temporale.
/// Occupazione sui giorni [startDay, endDay): la banchina torna libera il giorno endDay.
///
/// Due foreign key:
///  - ShipId  (1:1, unica): una nave ha al massimo una assegnazione.
///  - BerthId (N:1): una banchina ospita piu' assegnazioni nel tempo.
/// </summary>
public class Assignment
{
    public long Id { get; set; }

    public long ShipId { get; set; }
    public Ship Ship { get; set; } = null!;

    public long BerthId { get; set; }
    public Berth Berth { get; set; } = null!;

    // Primo giorno di occupazione (incluso).
    public int StartDay { get; set; }

    // Primo giorno libero dopo l'occupazione (escluso). EndDay = StartDay + durata.
    public int EndDay { get; set; }

    public Assignment()
    {
    }

    public Assignment(Ship ship, Berth berth, int startDay, int endDay)
    {
        Ship = ship;
        ShipId = ship.Id;
        Berth = berth;
        BerthId = berth.Id;
        StartDay = startDay;
        EndDay = endDay;
    }
}
