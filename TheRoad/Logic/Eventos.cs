using TheRoad.Models;

namespace TheRoad.Logic;

public static class Eventos
{
    private static readonly DataService.EventsData _events = GameData.Events;
    private static readonly Random _rnd = new();

    public static string? EventoGrande(GameState s) => AplicarEvento(s, _events.BigEvents);

    public static string? EventoMenor(GameState s) => AplicarEvento(s, _events.MinorEvents);

    public static string? Chapuceo(GameState s)
    {
        if (_events.ScavengeEvents.Count == 0) return null;
        var evt = _events.ScavengeEvents[_rnd.Next(_events.ScavengeEvents.Count)];
        return AplicarEvento(s, [evt]);
    }

    /// <summary>Resuelve la opción elegida de un evento con decisión: aplica sus efectos y
    /// devuelve el texto de consecuencia. Índice inválido o evento malformado: null sin mutar.</summary>
    public static string? ResolverDecision(GameState s, DataService.DecisionDto? ev, int indice)
    {
        if (ev == null || ev.Options.Count == 0 || indice < 0 || indice >= ev.Options.Count)
            return null;
        var op = ev.Options[indice];
        if (op?.Effects == null || string.IsNullOrEmpty(op.Result)) return null;
        return op.Result + AplicarEfectos(s, op.Effects);
    }

    /// <summary>Aplica unos efectos al estado y devuelve los sufijos de hambruna/inventario.
    /// Lógica única compartida por eventos clásicos y decisiones.</summary>
    public static string AplicarEfectos(GameState s, DataService.EventEffects e)
    {
        s.Player.HP = Math.Clamp(s.Player.HP + e.Hp, 0, ItemsService.MaxHP);
        s.Player.Comida = Math.Clamp(s.Player.Comida + e.Food, 0, ItemsService.MaxComida);
        s.Player.Agua = Math.Clamp(s.Player.Agua + e.Water, 0, ItemsService.MaxAgua);
        s.Player.Medicina = Math.Clamp(s.Player.Medicina + e.Medicine, 0, ItemsService.MaxMedicina);
        s.Player.Combustible = Math.Clamp(s.Player.Combustible + e.Fuel, 0, ItemsService.MaxCombustible);
        s.Vehiculo = Math.Clamp(s.Vehiculo + e.Vehicle, 0, VehicleService.MaxEstado);
        s.Dia += e.Days;

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

    private static string? AplicarEvento(GameState s, List<DataService.EventDto> eventos)
    {
        if (eventos.Count == 0) return null;
        var evt = eventos[_rnd.Next(eventos.Count)];
        var e = evt?.Effects;
        if (e == null) return null; // Evento malformado en JSON: se omite sin tumbar el viaje.

        return evt.Text + AplicarEfectos(s, e);
    }
}
