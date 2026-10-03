using TheRoad.Models;

namespace TheRoad.Logic;

// Estado y reparación del vehículo. Lógica pura, sin UI.
public static class VehicleService
{
    public const int MaxEstado = 100;
    public const int EstadoInicial = 80;

    /// <summary>Chatarra necesaria: 1 unidad = +20 hasta 100.</summary>
    public const int ReparacionPorChatarra = 20;

    /// <summary>Por debajo de este estado se avisa al jugador.</summary>
    public const int UmbralAviso = 30;

    /// <summary>Daño fijo de una avería en ruta.</summary>
    public const int DanoAveria = 10;

    /// <summary>% máximo de avería con el vehículo al mínimo. A 100 de salud es 0%.</summary>
    public const int AveriaMax = 30;

    public static bool HayAveria(int estadoPrevio)
        => Random.Shared.Next(1, 101) <= ProbabilidadAveria(estadoPrevio);

    /// <summary>Desgaste determinista por viaje: 2 + riesgo (3-5).</summary>
    public static int DesgastePorViaje(Route r) => 2 + r.Riesgo;

    /// <summary>Probabilidad de avería (%) lineal: 0% a 100 de salud, sube al bajar la salud.</summary>
    public static int ProbabilidadAveria(int estado)
        => Math.Clamp((MaxEstado - estado) * AveriaMax / MaxEstado, 0, AveriaMax);

    /// <summary>Null = puede viajar. A 0 el coche no anda.</summary>
    public static string? PuedeViajar(GameState s)
        => s.Vehiculo <= 0
            ? "El coche no anda (0/100). Repáralo con chatarra en la pestaña Vehículo."
            : null;

    public static int ChatarraCount(Player p)
        => p.Inventario.Count(x => x == "Chatarra");

    /// <summary>Repara con 1 chatarra (+20, cap 100). Null si no hay chatarra.</summary>
    public static string? Reparar(Player p, GameState s)
    {
        if (!p.Inventario.Remove("Chatarra")) return null;
        s.Vehiculo = Math.Min(MaxEstado, s.Vehiculo + ReparacionPorChatarra);
        return $"Aprietas tuercas y cambias piezas (+{ReparacionPorChatarra} vehículo: {s.Vehiculo}/{MaxEstado}).";
    }
}
