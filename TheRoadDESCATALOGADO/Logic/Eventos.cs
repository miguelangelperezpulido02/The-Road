using TheRoad.Models;

namespace TheRoad.Logic;

public static class Eventos
{
    private static readonly DataService.EventsData _events = GameData.Events;
    private static readonly Random _rnd = new();

    public static string EventoGrande(GameState s) => AplicarEvento(s, _events.BigEvents);

    public static string EventoMenor(GameState s) => AplicarEvento(s, _events.MinorEvents);

    public static string? Chapuceo(GameState s)
    {
        var evt = _events.ScavengeEvents[_rnd.Next(_events.ScavengeEvents.Count)];
        return AplicarEvento(s, [evt]);
    }

    private static string AplicarEvento(GameState s, List<DataService.EventDto> eventos)
    {
        var evt = eventos[_rnd.Next(eventos.Count)];
        var e = evt.Effects;

        s.Player.HP = Math.Max(0, s.Player.HP + e.Hp);
        s.Player.Comida = Math.Max(0, s.Player.Comida + e.Food);
        s.Player.Agua = Math.Max(0, s.Player.Agua + e.Water);
        s.Player.Medicina = Math.Max(0, s.Player.Medicina + e.Medicine);
        s.Dia += e.Days;

        bool inventarioLleno = false;
        foreach (var item in e.Inventory)
        {
            if (s.Player.Inventario.Count >= ItemsService.Capacidad)
            {
                inventarioLleno = true;
                break;
            }
            s.Player.Inventario.Add(item);
        }

        return inventarioLleno
            ? evt.Text + " (El inventario está lleno: algo se ha quedado atrás.)"
            : evt.Text;
    }
}
