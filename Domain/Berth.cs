namespace BlueHarbor.Domain;

/// <summary>
/// Banchina del terminal. Insieme fisso e immutabile (seed all'avvio):
/// 1 XL, 1 L, 2 M, 4 S. Ogni banchina ospita solo navi della propria dimensione.
/// </summary>
public class Berth
{
    public long Id { get; set; }

    // Codice leggibile, es. "XL1", "M2".
    public string Code { get; set; } = string.Empty;

    public Size Size { get; set; }

    public Berth()
    {
    }

    public Berth(string code, Size size)
    {
        Code = code;
        Size = size;
    }
}
