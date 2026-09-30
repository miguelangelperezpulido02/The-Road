using TheRoad.Models;

namespace TheRoad.Logic;

// Uso de objetos del inventario. Devuelve el texto del efecto o null si no es usable.
public static class ItemsService
{
    public const int GasolinaPorBidon = 5;
    public const int HPporVendaje = 10;

    /// <summary>Techos de recursos (alineados con las barras del HUD).</summary>
    public const int MaxHP = 100;
    public const int MaxCombustible = 100;
    public const int MaxComida = 20;
    public const int MaxAgua = 20;
    public const int MaxMedicina = 10;

    /// <summary>Huecos físicos del inventario (8x8).</summary>
    public const int Capacidad = 64;

    public static bool EsUsable(string item)
        => item is "Bidón de gasolina" or "Vendaje" or "Lata de conservas";

    public static string? Usar(string item, Player p)
    {
        if (!EsUsable(item)) return null;
        if (!p.Inventario.Remove(item)) return null;

        switch (item)
        {
            case "Bidón de gasolina":
                p.Combustible = Math.Min(MaxCombustible, p.Combustible + GasolinaPorBidon);
                return $"Vacas el bidón en el depósito (+{GasolinaPorBidon} gasolina).";
            case "Vendaje":
                p.HP = Math.Min(MaxHP, p.HP + HPporVendaje);
                return $"Te vendas las heridas (+{HPporVendaje} HP).";
            case "Lata de conservas":
                p.Comida = Math.Min(MaxComida, p.Comida + 1);
                return "Te comes la conserva (+1 comida).";
            default:
                return null;
        }
    }
}
