using TheRoad.Models;

namespace TheRoad.Logic;

public static class Eventos
{
    private static readonly DataService.EventsData _events = GameData.Events;
    private static readonly Random _rnd = new();

    /// <summary>Valor actual de un recurso por nombre (hp/food/water/medicine/fuel/vehicle).</summary>
    public static int ValorRecurso(GameState s, string recurso) => recurso switch
    {
        "hp" => s.Player.HP,
        "food" => s.Player.Comida,
        "water" => s.Player.Agua,
        "medicine" => s.Player.Medicina,
        "fuel" => s.Player.Combustible,
        "vehicle" => s.Vehiculo,
        _ => int.MaxValue, // Recurso desconocido: no bloquea.
    };

    public static string NombreRecurso(string recurso) => recurso switch
    {
        "hp" => "HP",
        "food" => "comida",
        "water" => "agua",
        "medicine" => "medicina",
        "fuel" => "gasolina",
        "vehicle" => "vehículo",
        _ => recurso,
    };

    /// <summary>¿Cumple la opción sus condiciones de marca? (para ocultar las que no tocan).</summary>
    public static bool CumpleMarcas(GameState s, DataService.DecisionOptionDto op)
        => (string.IsNullOrEmpty(op.RequiereMarca) || s.Marcas.Contains(op.RequiereMarca))
        && (string.IsNullOrEmpty(op.SinMarca) || !s.Marcas.Contains(op.SinMarca));

    /// <summary>¿Cumple la opción su requisito de recurso? (para deshabilitar sin ocultar).</summary>
    public static bool CumpleRecurso(GameState s, DataService.DecisionOptionDto op)
        => op.Requiere == null || ValorRecurso(s, op.Requiere.Recurso) >= op.Requiere.Min;

    public static bool OpcionDisponible(GameState s, DataService.DecisionOptionDto op)
        => CumpleMarcas(s, op) && CumpleRecurso(s, op);

    /// <summary>¿Coincide el evento con el momento del día? (null/vacío: cualquier momento).</summary>
    public static bool CoincideMomento(GameState? s, DataService.DecisionDto d)
        => s == null || string.IsNullOrEmpty(d.Momento)
        || d.Momento == (s.EsDeNoche ? "noche" : "dia");

    /// <summary>Sortea una decisión válida de un pool, filtrando por SoloSi, marcas y momento.
    /// Si el momento vacía el pool, se re-sortea entre los de cualquier momento. Null si vacío.</summary>
    private static DataService.DecisionDto? Sortear(List<DataService.DecisionDto> pool, GameState? s = null)
    {
        var base_ = pool
            .Where(d => d != null && !string.IsNullOrEmpty(d.Text) && d.Options.Count > 0
                && (s == null || d.SoloSi == null || ValorRecurso(s, d.SoloSi.Recurso) <= d.SoloSi.Max)
                && (s == null || string.IsNullOrEmpty(d.RequiereMarca) || s.Marcas.Contains(d.RequiereMarca))
                && (s == null || string.IsNullOrEmpty(d.SinMarca) || !s.Marcas.Contains(d.SinMarca)))
            .ToList();
        var lista = base_.Where(d => CoincideMomento(s, d)).ToList();
        if (lista.Count == 0)
            lista = base_.Where(d => string.IsNullOrEmpty(d.Momento)).ToList();
        if (lista.Count == 0) return null;
        return lista[_rnd.Next(lista.Count)];
    }

    /// <summary>Sortea una decisión de Segmentado (pool Oleada 2). Null si está vacío.</summary>
    public static DataService.DecisionDto? DecisionMenor(GameState? s = null) => Sortear(_events.DecisionEvents, s);

    /// <summary>Sortea una decisión de rebusca en parada (pool Oleada 3). Null si está vacío.</summary>
    public static DataService.DecisionDto? DecisionRebusca(GameState? s = null) => Sortear(_events.DecisionScavenge, s);

    /// <summary>Sortea un evento grande con decisión (pool Oleada 1). Null si el pool está vacío.</summary>
    public static DataService.DecisionDto? DecisionGrande(GameState? s = null) => Sortear(_events.DecisionBig, s);

    /// <summary>Resuelve la opción elegida de un evento con decisión: aplica sus efectos y
    /// devuelve el texto de consecuencia. Índice inválido o evento malformado: null sin mutar.</summary>
    public static string? ResolverDecision(GameState s, DataService.DecisionDto? ev, int indice)
    {
        if (ev == null || ev.Options.Count == 0 || indice < 0 || indice >= ev.Options.Count)
            return null;
        var op = ev.Options[indice];
        if (op?.Effects == null || string.IsNullOrEmpty(op.Result)) return null;
        if (!OpcionDisponible(s, op)) return null; // Revalida: requisitos y marcas vigentes.
        return op.Result + AplicarEfectos(s, op.Effects);
    }

    /// <summary>Aplica unos efectos al estado y devuelve los sufijos de hambruna/inventario.
    /// Lógica única compartida por eventos clásicos y decisiones.</summary>
    public static string AplicarEfectos(GameState s, DataService.EventEffects e)
    {
        s.Player.HP = Math.Clamp(s.Player.HP + e.Hp, 0, ItemsService.MaxHP);        s.Player.Comida = Math.Clamp(s.Player.Comida + e.Food, 0, ItemsService.MaxComida);
        s.Player.Agua = Math.Clamp(s.Player.Agua + e.Water, 0, ItemsService.MaxAgua);
        s.Player.Medicina = Math.Clamp(s.Player.Medicina + e.Medicine, 0, ItemsService.MaxMedicina);
        s.Player.Combustible = Math.Clamp(s.Player.Combustible + e.Fuel, 0, ItemsService.MaxCombustible);
        s.Vehiculo = Math.Clamp(s.Vehiculo + e.Vehicle, 0, VehicleService.MaxEstado);
        s.Dia += e.Days;
        foreach (var m in e.Marcas ?? [])
            if (!string.IsNullOrEmpty(m)) s.Marcas.Add(m);

        // Los días que salta el evento también cuentan para la hambruna.
        int danoHambruna = ItemsService.AplicarHambruna(s.Player, e.Days);
        string extraHambruna = danoHambruna > 0
            ? $" ({e.Days} día(s) sin comer: -{danoHambruna} HP por hambruna.)"
            : string.Empty;

        bool inventarioLleno = false;
        foreach (var item in e.Inventory ?? [])
        {
            if (s.Player.Inventario.Count >= ItemsService.Capacidad)
            {
                inventarioLleno = true;
                break;
            }
            s.Player.Inventario.Add(item);
        }

        return inventarioLleno
            ? " (El inventario está lleno: algo se ha quedado atrás.)" + extraHambruna
            : extraHambruna;
    }
}
