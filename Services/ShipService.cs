using BlueHarbor.Data;
using BlueHarbor.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlueHarbor.Services;

/// <summary>
/// Casi d'uso dell'Operatore: creazione e manutenzione delle navi.
/// </summary>
public class ShipService
{
    // Regole di dominio per la generazione automatica.
    private const int MaxArrivalOffset = 30; // non oltre 30 giorni dal giorno corrente
    private const int MinDuration = 3;
    private const int MaxDuration = 15;

    private readonly AppDbContext _db;
    private readonly TerminalClockService _clockService;
    private readonly Random _random = new();

    public ShipService(AppDbContext db, TerminalClockService clockService)
    {
        _db = db;
        _clockService = clockService;
    }

    /// <summary>
    /// Crea una nave in stato Pending. Il sistema assegna automaticamente
    /// dimensione, giorno di arrivo e durata di occupazione; l'Operatore fornisce
    /// solo nome e note.
    /// </summary>
    public Ship Create(string name, string? notes)
    {
        int currentDay = _clockService.GetCurrentDay();

        var ship = new Ship
        {
            Name = name,
            Notes = notes,
            Size = RandomSize(),
            ArrivalDay = currentDay + _random.Next(MaxArrivalOffset + 1),                 // currentDay .. +30
            OccupationDuration = MinDuration + _random.Next(MaxDuration - MinDuration + 1), // 3 .. 15
            Status = ShipStatus.PENDING
        };

        _db.Ships.Add(ship);
        _db.SaveChanges();
        return ship;
    }

    public List<Ship> FindAll()
    {
        return _db.Ships
            .OrderBy(s => s.ArrivalDay)
            .ThenBy(s => s.Id)
            .ToList();
    }

    public Ship Get(long id)
    {
        return _db.Ships.FirstOrDefault(s => s.Id == id)
               ?? throw DomainException.NotFound($"Nave non trovata: {id}");
    }

    /// <summary>
    /// L'Operatore mantiene le informazioni della nave finche' e' in stato Pending.
    /// Dopo l'assegnazione non sono ammesse modifiche (fuori scope).
    /// </summary>
    public Ship UpdateMetadata(long id, string name, string? notes)
    {
        Ship ship = Get(id);
        if (ship.Status != ShipStatus.PENDING)
        {
            throw DomainException.Conflict("La nave puo' essere modificata solo in stato Pending");
        }
        ship.Name = name;
        ship.Notes = notes;
        _db.SaveChanges();
        return ship;
    }

    private Size RandomSize()
    {
        var values = Enum.GetValues<Size>();
        return values[_random.Next(values.Length)];
    }
}
