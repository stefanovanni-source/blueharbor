namespace BlueHarbor.Domain;

/// <summary>
/// Ciclo di vita minimale di una nave:
/// PENDING  -> in attesa di assegnazione (stato iniziale)
/// ASSIGNED -> banchina assegnata
/// DEPARTED -> occupazione terminata (conclusa ai fini dell'esercizio)
/// </summary>
public enum ShipStatus
{
    PENDING,
    ASSIGNED,
    DEPARTED
}
