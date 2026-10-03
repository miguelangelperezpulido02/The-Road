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

    /// <summary>HP perdidos por cada día pasado sin comida (Comida == 0).</summary>
    public const int HambrunaPorDia = 5;

    /// <summary>HP perdidos por cada viaje sin comida en el inventario.</summary>
    public const int HambrePorViaje = 2;

    /// <summary>HP perdidos por cada viaje sin agua en el inventario.</summary>
    public const int SedPorViaje = 5;

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

    /// <summary>Aplica el daño por hambruna: HambrunaPorDia HP por cada día pasado sin comida.
    /// Solo cuenta si Comida == 0. Devuelve el HP perdido (nunca baja de 0).</summary>
    public static int AplicarHambruna(Player p, int dias)
    {
        if (dias <= 0 || p.Comida > 0) return 0;
        int dano = Math.Min(p.HP, HambrunaPorDia * dias);
        p.HP -= dano;
        return dano;
    }
}
