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

        s.Player.HP = Math.Clamp(s.Player.HP + e.Hp, 0, ItemsService.MaxHP);
        s.Player.Comida = Math.Clamp(s.Player.Comida + e.Food, 0, ItemsService.MaxComida);
        s.Player.Agua = Math.Clamp(s.Player.Agua + e.Water, 0, ItemsService.MaxAgua);
        s.Player.Medicina = Math.Clamp(s.Player.Medicina + e.Medicine, 0, ItemsService.MaxMedicina);
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
