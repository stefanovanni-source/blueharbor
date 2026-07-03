using BlueHarbor.Domain;

namespace BlueHarbor.Services;

/// <summary>
/// Vista dello stato di una banchina per la board dello Scheduler:
/// occupata oggi?, primo giorno libero, e l'elenco delle finestre gia' pianificate.
/// </summary>
public record BerthOccupancy(
    long BerthId,
    string Code,
    Size Size,
    bool OccupiedNow,
    int NextFreeDay,
    IReadOnlyList<OccupiedWindow> Windows);
