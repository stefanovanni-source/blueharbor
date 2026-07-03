using System.Net;

namespace BlueHarbor.Services;

/// <summary>
/// Eccezione di dominio con codice HTTP e motivo leggibile.
/// I controller la intercettano per mostrare un messaggio d'errore all'utente.
/// </summary>
public class DomainException : Exception
{
    public HttpStatusCode Status { get; }

    public DomainException(HttpStatusCode status, string reason) : base(reason)
    {
        Status = status;
    }

    public static DomainException NotFound(string reason) => new(HttpStatusCode.NotFound, reason);

    public static DomainException Conflict(string reason) => new(HttpStatusCode.Conflict, reason);
}
