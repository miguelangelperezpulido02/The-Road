using TheRoad.Models;

namespace TheRoad.Logic;

// Uso de objetos del inventario. Devuelve el texto del efecto o null si no es usable.
public static class ItemsService
{
    public const int GasolinaPorBidon = 5;
    public const int HPporVendaje = 10;

    public static bool EsUsable(string item)
        => item is "Bidón de gasolina" or "Vendaje" or "Lata de conservas";

    public static string? Usar(string item, Player p)
    {
        if (!p.Inventario.Remove(item)) return null;

        switch (item)
        {
            case "Bidón de gasolina":
                p.Combustible += GasolinaPorBidon;
                return $"Vacas el bidón en el depósito (+{GasolinaPorBidon} gasolina).";
            case "Vendaje":
                p.HP = Math.Min(100, p.HP + HPporVendaje);
                return $"Te vendas las heridas (+{HPporVendaje} HP).";
            case "Lata de conservas":
                p.Comida += 1;
                return "Te comes la conserva (+1 comida).";
            default:
                return null;
        }
    }
}
