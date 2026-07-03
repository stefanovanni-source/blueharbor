namespace BlueHarbor.Services;

/// <summary>Una finestra di occupazione di una banchina: giorni [startDay, endDay).</summary>
public record OccupiedWindow(string ShipName, int StartDay, int EndDay);
