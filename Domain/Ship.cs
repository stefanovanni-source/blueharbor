namespace BlueHarbor.Domain;

/// <summary>
/// Nave in arrivo al terminal.
/// Dimensione, giorno di arrivo e durata di occupazione sono generati
/// automaticamente alla creazione; nome e note sono inseriti dall'Operatore.
/// </summary>
public class Ship
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Note libere (max 1000 caratteri).
    public string? Notes { get; set; }

    public Size Size { get; set; }

    // Giorno virtuale di arrivo (non oltre 30 giorni dal giorno corrente alla creazione).
    public int ArrivalDay { get; set; }

    // Durata di occupazione della banchina, in giorni (3..15).
    public int OccupationDuration { get; set; }

    public ShipStatus Status { get; set; } = ShipStatus.PENDING;
}
