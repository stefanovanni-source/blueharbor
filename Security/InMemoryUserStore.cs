namespace BlueHarbor.Security;

/// <summary>
/// Utente demo in-memory (username, password in chiaro, ruolo).
/// </summary>
public record DemoUser(string Username, string Password, string Role);

/// <summary>
/// Sicurezza e ruoli.
///
/// Assunzione (ammessa dalla traccia): autenticazione minimale con due utenti
/// in-memory, uno per ruolo. Password in chiaro perche' l'esercizio e'
/// didattico e la gestione utenti e' fuori scope.
///
///   operatore / operatore  -> ROLE OPERATORE
///   scheduler / scheduler  -> ROLE SCHEDULER
/// </summary>
public class InMemoryUserStore
{
    private readonly List<DemoUser> _users = new()
    {
        new DemoUser("operatore", "operatore", Roles.Operatore),
        new DemoUser("scheduler", "scheduler", Roles.Scheduler)
    };

    /// <summary>Restituisce l'utente se le credenziali sono valide, altrimenti null.</summary>
    public DemoUser? Validate(string username, string password)
    {
        return _users.FirstOrDefault(u =>
            u.Username == username && u.Password == password);
    }
}

/// <summary>Nomi dei ruoli usati per l'autorizzazione.</summary>
public static class Roles
{
    public const string Operatore = "OPERATORE";
    public const string Scheduler = "SCHEDULER";
}
