namespace BlueHarbor.Domain;

/// <summary>
/// Orologio virtuale del terminal: singola riga (id = 1) che mantiene il giorno corrente.
/// Il sistema non e' real-time: l'azione "Next Day" incrementa questo valore.
/// </summary>
public class TerminalClock
{
    public long Id { get; set; }

    public int CurrentDay { get; set; }

    public TerminalClock()
    {
    }

    public TerminalClock(long id, int currentDay)
    {
        Id = id;
        CurrentDay = currentDay;
    }
}
